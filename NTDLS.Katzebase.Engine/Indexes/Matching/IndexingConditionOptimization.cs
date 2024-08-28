using NTDLS.Helpers;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Query;
using NTDLS.Katzebase.Engine.Query.Constraints;
using NTDLS.Katzebase.Engine.Schemas;
using NTDLS.Katzebase.Shared;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Indexes.Matching
{
    internal class IndexingConditionOptimization
    {
        /// <summary>
        /// Contains a list of nested operations that will be used for indexing operations.
        /// </summary>
        public List<IndexingConditionGroup> IndexingConditionGroup { get; set; } = new();

        /// <summary>
        /// A clone of the conditions that this optimization was built for.
        /// Also contains the indexes associated with each SubCondition of conditions.
        /// </summary>
        public Conditions Conditions { get; private set; }

        public Transaction Transaction { get; private set; }

        public IndexingConditionOptimization(Transaction transaction, Conditions conditions)
        {
            Transaction = transaction;
            Conditions = conditions.Clone();
        }

        #region Builder.

        /// <summary>
        /// Takes a nested set of conditions and returns a clone of the conditions with associated selection of indexes.
        /// </summary>
        public static IndexingConditionOptimization BuildTree(EngineCore core, Transaction transaction,
            PhysicalSchema physicalSchema, Conditions allConditions, string workingSchemaPrefix)
        {
            var optimization = new IndexingConditionOptimization(transaction, allConditions);

            var indexCatalog = core.Indexes.AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

            if (optimization.Conditions.Root.ExpressionKeys.Count > 0)
            {
                //The root condition is just a pointer to a child condition, so get the "root" child condition.
                var rootCondition = optimization.Conditions.SubConditionFromExpressionKey(optimization.Conditions.Root.Key);

                if (!BuildTree(optimization, core, transaction, indexCatalog, physicalSchema, workingSchemaPrefix, rootCondition, optimization.IndexingConditionGroup))
                {
                    //Invalidate indexing optimization.
                    return new IndexingConditionOptimization(transaction, allConditions);
                }
            }

            return optimization;
        }

        /// <summary>
        /// Takes a nested set of conditions and returns a clone of the conditions with associated selection of indexes.
        /// Called reclusively by BuildTree().
        /// </summary>
        private static bool BuildTree(IndexingConditionOptimization optimization, EngineCore core, Transaction transaction, PhysicalIndexCatalog indexCatalog,
            PhysicalSchema physicalSchema, string workingSchemaPrefix, SubCondition givenSubCondition, List<IndexingConditionGroup> indexingConditionGroups)
        {
            foreach (var expressionKey in givenSubCondition.ExpressionKeys)
            {
                var subCondition = optimization.Conditions.SubConditionFromExpressionKey(expressionKey);

                if (subCondition.Conditions.Count > 0)
                {
                    if (subCondition.LogicalConnector == LogicalConnector.Or)
                    {
                        if (subCondition.Conditions.Any(o => o.Left.Prefix.Is(workingSchemaPrefix)) == false
                            && subCondition.Conditions.Any(o => o.Right.Prefix.Is(workingSchemaPrefix)) == false)
                        {
                            //Each "OR" condition group must have at least one potential indexable match for the selected schema,
                            //  this is because we need to be able to create a full list of all possible documents for this schema,
                            //  and if we have an "OR" group that does not further limit these documents by the given schema then
                            //  we will have to do a full namespace scan anyway.
                            return false; //Invalidate indexing optimization.
                        }
                    }

                    bool locatedGroupIndex = false;

                    //Loop through all indexes, all their attributes and all conditions in this sub-condition
                    //  for the given schema. Keep track of which indexes match each condition field.
                    foreach (var physicalIndex in indexCatalog.Collection)
                    {
                        //Console.WriteLine($"Considering index: {physicalIndex.Name}");

                        bool locatedIndexAttribute = false;

                        var indexSelection = new IndexSelection(physicalIndex);
                        foreach (var attribute in physicalIndex.Attributes)
                        {
                            locatedIndexAttribute = false;
                            foreach (var condition in subCondition.Conditions.Where(o => o.Left.Prefix.Is(workingSchemaPrefix)))
                            {
                                if (condition.Left.Value?.Is(attribute.Field) == true)
                                {
                                    indexSelection.CoveredConditions.Add(condition);
                                    //Console.WriteLine($"Indexed: {condition.ConditionKey} is ({condition.Left} {condition.LogicalQualifier} {condition.Right})");
                                    locatedIndexAttribute = true;
                                    locatedGroupIndex = true;
                                }
                            }

                            if (locatedIndexAttribute == false)
                            {
                                //We want to match the index attributes in the order which they appear in the index, so if we find 
                                //  one index attribute that we can not match to a condition then we need to break. In this case, we 
                                //  will either use a better index (if found) or use this partial index match with leaf-distillation.
                                break;
                            }
                        }

                        if (indexSelection.CoveredConditions.Count > 0)
                        {
                            //We either have a full or a partial index match.
                            indexSelection.IsFullIndexMatch = indexSelection.CoveredConditions.Count == indexSelection.Index.Attributes.Count;

                            subCondition.IndexSelections.Add(indexSelection);
                        }
                    }

                    if (locatedGroupIndex == false)
                    {
                        //This group has no indexing, but since it does reference the
                        //  given schema, we are going to have to do a full schema scan.
                        return false; //Invalidate indexing optimization.
                    }

                    foreach (var indexSelection in subCondition.IndexSelections)
                    {
                        //Console.WriteLine($"{indexSelection.Index.Name}, CoveredFields: ({indexSelection.CoveredFields.Count})");
                    }
                }

                if (subCondition.IndexSelections.Count > 0)
                {
                    IndexingConditionGroup indexingConditionGroup = new(subCondition.LogicalConnector);

                    //At this point, we have settled on the possible indexes for the conditions in this expression, now we need
                    //to create some kind of new "indexing operation" class where we layout exactly how each condition will be used.
                    //Fist lets find out if we have any fields that are using composite indexes where we have the fields to satisfy it.

                    #region Composite index matching.

                    while (true)
                    {
                        var compositeIndex = subCondition
                            .IndexSelections.Where(o => o.CoveredConditions.Count(c => c.IsIndexOptimized == false) > 1 && o.IsFullIndexMatch)
                            .OrderByDescending(o => o.CoveredConditions.Count).FirstOrDefault();

                        List<Condition> conditionsOptimizedByThisIndex = new();

                        if (compositeIndex != null)
                        {
                            IndexingConditionLookup indexingConditionLookup = new(compositeIndex.Index);

                            //Find the condition fields that match the index attributes.
                            foreach (var attribute in compositeIndex.Index.Attributes)
                            {
                                var matchedConditions = subCondition.Conditions
                                    .Where(o => o.Left.Prefix == workingSchemaPrefix
                                    && o.IsIndexOptimized == false && o.Left.Value?.Is(attribute.Field) == true).ToList();

                                if (matchedConditions.Count == 0)
                                {
                                    //No condition fields were found for this index attribute.
                                    break;
                                }

                                foreach (var condition in matchedConditions)
                                {
                                    //Set these matched conditions as IsIndexOptimized so that we can
                                    //  break out of this loop when we run out of conditions to evaluate.
                                    condition.IsIndexOptimized = true;
                                    conditionsOptimizedByThisIndex.Add(condition);
                                }

                                indexingConditionLookup.AttributeConditionSets.Add(attribute.Field.EnsureNotNull(), matchedConditions);
                            }

                            if (indexingConditionLookup.AttributeConditionSets.Count > 0)
                            {
                                indexingConditionGroup.Lookups.Add(indexingConditionLookup);
                            }
                        }
                        else
                        {
                            //No suitable composite index was found.
                            //Make the conditions available for other indexing operations.
                            foreach (var condition in conditionsOptimizedByThisIndex)
                            {
                                condition.IsIndexOptimized = false;
                            }
                            break;
                        }
                    }

                    #endregion

                    #region non-Composite index matching (exact).

                    while (true)
                    {
                        //First try to match conditions to exact non-composite indexes, meaning indexes
                        //  that have only one attribute and that attribute matches the condition field.
                        var nonCompositeIndex = subCondition
                            .IndexSelections.Where(o => o.CoveredConditions.Count(c => c.IsIndexOptimized == false) == 1 && o.IsFullIndexMatch)
                            .FirstOrDefault();

                        List<Condition> conditionsOptimizedByThisIndex = new();

                        if (nonCompositeIndex != null)
                        {
                            IndexingConditionLookup indexingConditionLookup = new(nonCompositeIndex.Index);

                            //Find the condition fields that match the index attributes.
                            foreach (var attribute in nonCompositeIndex.Index.Attributes)
                            {
                                var matchedConditions = subCondition.Conditions
                                    .Where(o => o.Left.Prefix == workingSchemaPrefix && o.IsIndexOptimized == false && o.Left.Value?.Is(attribute.Field) == true).ToList();

                                if (matchedConditions.Count == 0)
                                {
                                    //No condition fields were found for this index attribute.
                                    break;
                                }

                                foreach (var condition in matchedConditions)
                                {
                                    //Set these matched conditions as IsIndexOptimized so that we can
                                    //  break out of this loop when we run out of conditions to evaluate.
                                    condition.IsIndexOptimized = true;
                                    conditionsOptimizedByThisIndex.Add(condition);
                                }

                                indexingConditionLookup.AttributeConditionSets.Add(attribute.Field.EnsureNotNull(), matchedConditions);
                            }

                            if (indexingConditionLookup.AttributeConditionSets.Count > 0)
                            {
                                indexingConditionGroup.Lookups.Add(indexingConditionLookup);
                            }
                        }
                        else
                        {
                            //No suitable composite index was found. 
                            //Make the conditions available for other indexing operations.
                            foreach (var condition in conditionsOptimizedByThisIndex)
                            {
                                condition.IsIndexOptimized = false;
                            }
                            break;
                        }
                    }

                    #endregion

                    #region non-Composite index matching (partial).

                    /*
                    I believe this is handled by the composite index matching.
                    while (true)
                    {
                        //First try to match conditions to non-exact composite indexes, meaning indexes
                        //  that have only one attribute and that attribute matches the condition field.
                        var nonCompositeIndex = subCondition
                            .IndexSelections.Where(o => o.CoveredConditions.Count(c => c.IsIndexOptimized == false) == 1)
                            .FirstOrDefault();

                        List<Condition> conditionsOptimizedByThisIndex = new();

                        if (nonCompositeIndex != null)
                        {
                            IndexingOperationConditions indexingOperationConditions = new(nonCompositeIndex.Index);

                            //Find the condition fields that match the index attributes.
                            foreach (var attribute in nonCompositeIndex.Index.Attributes)
                            {
                                var matchedConditions = subCondition.Conditions
                                    .Where(o => o.Left.Prefix == workingSchemaPrefix && o.IsIndexOptimized == false && o.Left.Value?.Is(attribute.Field) == true).ToList();

                                if (matchedConditions.Count == 0)
                                {
                                    //No condition fields were found for this index attribute.
                                    break;
                                }

                                foreach (var condition in matchedConditions)
                                {
                                    //Set these matched conditions as IsIndexOptimized so that we can
                                    //  break out of this loop when we run out of conditions to evaluate.
                                    condition.IsIndexOptimized = true;
                                    conditionsOptimizedByThisIndex.Add(condition);
                                }

                                indexingOperationConditions.Conditions.Add(attribute.Field.EnsureNotNull(), matchedConditions);
                            }

                            if (indexingOperationConditions.Conditions.Count > 0)
                            {
                                indexingOperation.Conditions.Add(indexingOperationConditions);
                            }
                        }
                        else
                        {
                            //No suitable composite index was found.

                            //Make the conditions available for other indexing operations.
                            foreach (var condition in conditionsOptimizedByThisIndex)
                            {
                                condition.IsIndexOptimized = false;
                            }
                            break;
                        }
                    }
                    */

                    #endregion

                    if (indexingConditionGroup.Lookups.Count > 0)
                    {
                        indexingConditionGroups.Add(indexingConditionGroup);
                    }

                    if (subCondition.ExpressionKeys.Count > 0)
                    {
                        if (!BuildTree(optimization, core, transaction, indexCatalog, physicalSchema,
                            workingSchemaPrefix, subCondition, indexingConditionGroup.SubIndexingConditionGroups))
                        {
                            return false; //Invalidate indexing optimization.
                        }
                    }
                }

                LogManager.Debug($"SubExpression: {subCondition.Expression}");
            }

            return true;
        }

        #endregion

        public static List<Condition> GetConvertedConditions(List<Condition> conditions, List<PrefixedField> coveredFields)
        {
            var result = new List<Condition>();

            foreach (var coveredField in coveredFields)
            {
                foreach (var condition in conditions)
                {
                    if (condition.Left.Key == coveredField.Key)
                    {
                        result.Add(condition);
                    }
                }
            }

            return result;
        }

        #region Optimization explanation.

        private static string Pad(int indentation) => "".PadLeft(indentation * 2, ' ');

        /// <summary>
        /// This function makes returns a string that represents how and where indexes are used to satisfy a query.
        /// </summary>
        public string ExplainPlan(EngineCore core, PhysicalSchema physicalSchema, IndexingConditionOptimization optimization, string workingSchemaPrefix)
        {
            var result = new StringBuilder();

            string schemaIdentifier = $"Schema: '{physicalSchema.Name}'";
            if (!string.IsNullOrEmpty(workingSchemaPrefix))
            {
                schemaIdentifier += $", alias: '{workingSchemaPrefix}'";
            }

            result.AppendLine("BEGIN>•••••••••••••••••••••••••••••••••••••••••••••••••••••");
            result.AppendLine($"• " + $"{schemaIdentifier}");
            result.AppendLine("•••••••••••••••••••••••••••••••••••••••••••••••••••••••••••");

            if (optimization.IndexingConditionGroup.Count == 0)
            {
                //No indexing on this condition group.
                result.AppendLine("• " + "Full schema scan:\r\n" + optimization.Conditions.ExplainFlat().Trim());
            }
            else
            {
                foreach (var indexingConditionGroup in optimization.IndexingConditionGroup) //Loop through the OR groups
                {
                    foreach (var lookup in indexingConditionGroup.Lookups) //Loop thorough the AND conditions.
                    {
                        ExplainLookupPlan(ref result, core, optimization, lookup, physicalSchema, workingSchemaPrefix);
                    }
                }
            }

            result.AppendLine("•••••••••••••••••••••••••••••••••••••••••••••••••••••••<END");

            return result.ToString();
        }

        private void ExplainLookupPlan(ref StringBuilder result, EngineCore core, IndexingConditionOptimization optimization,
            IndexingConditionLookup lookup, PhysicalSchema physicalSchema, string workingSchemaPrefix)
        {
            try
            {
                var conditionSet = lookup.AttributeConditionSets[lookup.Index.Attributes[0].Field.EnsureNotNull()];

                foreach (var condition in conditionSet)
                {
                    ExplainLookupCondition(ref result, core, optimization, lookup, physicalSchema, workingSchemaPrefix, condition);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to match index documents for process id {optimization.Transaction.ProcessId}.", ex);
                throw;
            }
        }

        private void ExplainLookupCondition(ref StringBuilder result, EngineCore core, IndexingConditionOptimization optimization, IndexingConditionLookup lookup,
            PhysicalSchema physicalSchema, string workingSchemaPrefix, Condition condition)
        {
            try
            {
                var indexAttribute = lookup.Index.Attributes[0].Field;

                if (condition.LogicalQualifier == LogicalQualifier.Equals)
                {
                    if (condition.Right.IsConstant)
                    {
                        result.AppendLine("• " + Pad(0) + $"Index seek of '{lookup.Index.Name}' on partition: {lookup.Index.ComputePartition(condition.Right.Value)}.");
                    }
                    else
                    {
                        result.AppendLine("• " + Pad(0) + $"Index seek of '{lookup.Index.Name}' on partition: single, determined at compile-time.");
                    }
                }
                else
                {
                    if (condition.Right.IsConstant)
                    {
                        result.AppendLine("• " + Pad(0) + $"Index scan of '{lookup.Index.Name}' on partitions: 1-{lookup.Index.Partitions}.");
                    }
                    else
                    {
                        result.AppendLine("• " + Pad(0) + $"Index scan of '{lookup.Index.Name}' on partitions: 1-{lookup.Index.Partitions}.");
                    }
                }

                if (condition.Right.IsConstant)
                {
                    result.AppendLine("• " + Pad(1) + $"'{indexAttribute}' on constant '{condition.Left.Key}' {condition.LogicalQualifier} '{condition.Right.Value}'.");
                }
                else
                {
                    result.AppendLine("• " + Pad(1) + $"'{indexAttribute}' on value of '{condition.Left.Key}' {condition.LogicalQualifier} '{condition.Right.Key}'.");
                }

                if (lookup.Index.Attributes.Count > 1)
                {
                    //Further, recursively, process additional compound index attribute condition matches.
                    ExplainLookupConditionRecursive(ref result, core, optimization,
                        lookup, physicalSchema, workingSchemaPrefix, condition, 1);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to match index by thread.", ex);
                throw;
            }
        }

        private static void ExplainLookupConditionRecursive(ref StringBuilder result,
            EngineCore core, IndexingConditionOptimization optimization, IndexingConditionLookup lookup,
            PhysicalSchema physicalSchema, string workingSchemaPrefix, Condition condition, int attributeDepth)
        {
            var conditionSet = lookup.AttributeConditionSets[lookup.Index.Attributes[attributeDepth].Field.EnsureNotNull()];

            foreach (var singleCondition in conditionSet)
            {
                var indexAttribute = lookup.Index.Attributes[attributeDepth].Field;

                if (attributeDepth == lookup.AttributeConditionSets.Count - 1)
                {
                    if (singleCondition.Right.IsConstant)
                    {
                        result.AppendLine("• " + Pad(1 + attributeDepth) + $"Leaf seek '{indexAttribute}' on constant '{singleCondition.Left.Key}' {singleCondition.LogicalQualifier} '{singleCondition.Right.Value}'.");
                    }
                    else
                    {
                        result.AppendLine("• " + Pad(1 + attributeDepth) + $"Leaf seek '{indexAttribute}' on value of '{singleCondition.Left.Key}' {singleCondition.LogicalQualifier} '{singleCondition.Right.Key}'.");
                    }

                }
                else if (attributeDepth < lookup.AttributeConditionSets.Count - 1)
                {
                    //Further, recursively, process additional compound index attribute condition matches.
                    ExplainLookupConditionRecursive(ref result, core, optimization, lookup,
                        physicalSchema, workingSchemaPrefix, condition, attributeDepth + 1);
                }
            }
        }

        #endregion

    }
}
