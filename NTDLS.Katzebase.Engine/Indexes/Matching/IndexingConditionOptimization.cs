using NTDLS.Helpers;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Parsers.Query.Class;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions.Helpers;
using NTDLS.Katzebase.Engine.QueryProcessing;
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
        public ConditionCollection Conditions { get; private set; }
        public Transaction Transaction { get; private set; }

        public IndexingConditionOptimization(Transaction transaction, ConditionCollection conditions)
        {
            Transaction = transaction;
            Conditions = conditions.Clone();
        }

        #region Builder.

        /// <summary>
        /// Takes a nested set of conditions and returns a clone of the conditions with associated selection of indexes.
        /// </summary>
        public static IndexingConditionOptimization BuildTree(EngineCore core, Transaction transaction, PreparedQuery query,
            PhysicalSchema physicalSchema, ConditionCollection conditions, string workingSchemaPrefix)
        {
            var optimization = new IndexingConditionOptimization(transaction, conditions);

            var indexCatalog = core.Indexes.AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

            if (!BuildTree(optimization, query, transaction, indexCatalog, workingSchemaPrefix, optimization.IndexingConditionGroup))
            {
                //Invalidate indexing optimization.
                return new IndexingConditionOptimization(transaction, conditions);
            }

            return optimization;
        }

        /// <summary>
        /// Takes a nested set of conditions and returns a clone of the conditions with associated selection of indexes.
        /// Called reclusively by BuildTree().
        /// </summary>
        private static bool BuildTree(IndexingConditionOptimization optimization, PreparedQuery query,
            Transaction transaction, PhysicalIndexCatalog indexCatalog, string workingSchemaPrefix,
            List<IndexingConditionGroup> indexingConditionGroups)
        {
            //We only flatten the condition groups because its easer to work with them this way,
            //    these are references to the optimizers copy of the nested groups, so changes
            //    made here affect the condition groups/entries (such as assigning indexes).
            var flattenedGroups = optimization.Conditions.FlattenToGroups();

            foreach (var flattenedGroup in flattenedGroups)
            {
                #region Build list of usable indexes.

                if (flattenedGroup.Entries.OfType<ConditionEntry>().Where(o => o.Left.SchemaAlias.Is(workingSchemaPrefix)).Any() == false)
                {
                    //Each "OR" condition group must have at least one potential indexable match for the selected schema,
                    //  this is because we need to be able to create a full list of all possible documents for this schema,
                    //  and if we have an "OR" group that does not further limit these documents by the given schema then
                    //  we will have to do a full namespace scan anyway.
                    if (flattenedGroup.Connector == LogicalConnector.Or)
                    {
                        return false; //Invalidate indexing optimization.
                    }
                    else
                    {
                        continue; //This group does not have conditions for the given schema, but this is an AND group, just skip the group.
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

                        List<ConditionEntry> applicableConditions = new List<ConditionEntry>();

                        if (string.IsNullOrEmpty(workingSchemaPrefix))
                        {
                            //For non-schema joins, we do not currently support indexing on anything other than constant expressions.
                            //However, I think this could be implemented pretty easily.
                            applicableConditions.AddRange(
                                flattenedGroup.Entries.OfType<ConditionEntry>()
                                .Where(o => o.Left.SchemaAlias.Is(workingSchemaPrefix) && StaticParserField.IsConstantExpression(o.Right.Value)));
                        }
                        else
                        {
                            //For schema joins, we already have a schema document value cache in the lookup functions so we allow non-constants.
                            applicableConditions.AddRange(
                                flattenedGroup.Entries.OfType<ConditionEntry>()
                                .Where(o => o.Left.SchemaAlias.Is(workingSchemaPrefix)));
                        }

                        foreach (var condition in applicableConditions)
                        {
                            if (condition.Left is QueryFieldDocumentIdentifier leftValue)
                            {
                                if (leftValue.FieldName?.Is(attribute.Field) == true)
                                {
                                    if (StaticParserField.IsConstantExpression(condition.Right.Value))
                                    {
                                        //To save time while indexing, we are going to collapse the value here if the expression does not contain non-constants.
                                        var constantValue = condition.Right.CollapseScalerQueryField(transaction, query, query.SelectFields, new())?.ToLowerInvariant();

                                        //TODO: Think about the nullability of constantValue.
                                        condition.Right = new QueryFieldCollapsedValue(constantValue.EnsureNotNull());
                                    }

                                    indexSelection.CoveredConditions.Add(condition);
                                    //Console.WriteLine($"Indexed: {condition.ConditionKey} is ({condition.Left} {condition.LogicalQualifier} {condition.Right})");
                                    locatedIndexAttribute = true;
                                    locatedGroupIndex = true;
                                }
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
                        indexSelection.IsFullIndexMatch = indexSelection.CoveredConditions.Select(o => o.Left.Value).Distinct().Count() == indexSelection.Index.Attributes.Count;

                        flattenedGroup.UsableIndexes.Add(indexSelection);
                    }
                }

                if (locatedGroupIndex == false)
                {
                    //This group has no indexing, but since it does reference the
                    //  given schema, we are going to have to do a full schema scan.
                    return false; //Invalidate indexing optimization.
                }

                #endregion

                #region Select the best indexes from the usable indexes.

                if (flattenedGroup.UsableIndexes.Count > 0)
                {
                    IndexingConditionGroup indexingConditionGroup = new(flattenedGroup.Connector);

                    var conditionsWithApplicableLeftDocumentIdentifiers = flattenedGroup.ThisLevelDocumentIdentifiers();

                    //Here we are ordering by the "full match" indexes first, then descending by the number of index attributes.
                    //The thinking being that we want to prefer full index matches that contain multiple index attributes
                    //  (composite indexes), then we want to look at non-composite full matches, and finally let any partial
                    //  match composite indexes pick up any remaining un-optimized conditions, but we want those to be ordered
                    //  ascending by the index attribute account because we want the smallest non-full-match composite indexes
                    //  because they require less work to distill later.

                    var preferenceOrderedIndexSelections = new List<IndexSelection>();

                    var fullMatches = flattenedGroup.UsableIndexes
                        .Where(o => o.IsFullIndexMatch == true && o.CoveredConditions.Count(c => c.IsIndexOptimized == false) > 0)
                        .OrderByDescending(o => o.CoveredConditions.Count).ToList();
                    preferenceOrderedIndexSelections.AddRange(fullMatches);

                    var partialMatches = flattenedGroup.UsableIndexes
                        .Where(o => o.IsFullIndexMatch == false && o.CoveredConditions.Count(c => c.IsIndexOptimized == false) > 0)
                        .OrderBy(o => o.CoveredConditions.Count).ToList();
                    preferenceOrderedIndexSelections.AddRange(partialMatches);

                    foreach (var indexSelection in preferenceOrderedIndexSelections)
                    {
                        var indexingConditionLookup = new IndexingConditionLookup(indexSelection.Index);

                        //Find the condition fields that match the index attributes.
                        foreach (var attribute in indexSelection.Index.Attributes)
                        {
                            //For non-schema joins, we do not currently support indexing on anything other than constant expressions.
                            //However, I think this could be implemented pretty easily.
                            //For schema joins, we already have a schema document value cache in the lookup functions so we allow non-constants.
                            //Which is th purpose of: "&& (o.Right is QueryFieldCollapsedValue || !string.IsNullOrEmpty(workingSchemaPrefix))"

                            var matchedConditions = conditionsWithApplicableLeftDocumentIdentifiers
                                .Where(o =>
                                       o.Left is QueryFieldDocumentIdentifier identifier
                                    && (o.Right is QueryFieldCollapsedValue || !string.IsNullOrEmpty(workingSchemaPrefix))
                                    && identifier.SchemaAlias.Is(workingSchemaPrefix)
                                    && o.IsIndexOptimized == false
                                    && identifier.FieldName.Is(attribute.Field) == true).ToList();

                            if (matchedConditions.Count == 0)
                            {
                                break; //No additional condition fields were found for this index attribute.
                            }

                            foreach (var condition in matchedConditions)
                            {
                                condition.IsIndexOptimized = true;
                            }

                            indexingConditionLookup.AttributeConditionSets.Add(attribute.Field.EnsureNotNull(), matchedConditions);
                        }

                        if (indexingConditionLookup.AttributeConditionSets.Count > 0)
                        {
                            indexingConditionGroup.Lookups.Add(indexingConditionLookup);
                        }
                    }

                    if (indexingConditionGroup.Lookups.Count > 0)
                    {
                        indexingConditionGroups.Add(indexingConditionGroup);
                    }
                }

                #endregion
            }

            return true;
        }

        #endregion

        #region Optimization explanation.

        private static string Pad(int indentation) => "".PadLeft(indentation * 2, ' ');

        /// <summary>
        /// This function makes returns a string that represents how and where indexes are used to satisfy a query.
        /// </summary>
        public string ExplainPlan(EngineCore core, PhysicalSchema physicalSchema, IndexingConditionOptimization optimization, string workingSchemaPrefix)
        {
            var result = new StringBuilder();

            /*
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
            */

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
                    //TODO: Reimplement.
                    //ExplainLookupCondition(ref result, core, optimization, lookup, physicalSchema, workingSchemaPrefix, condition);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to match index documents for process id {optimization.Transaction.ProcessId}.", ex);
                throw;
            }
        }

        private void ExplainLookupCondition(ref StringBuilder result, EngineCore core, IndexingConditionOptimization optimization, IndexingConditionLookup lookup,
            PhysicalSchema physicalSchema, string workingSchemaPrefix, ConditionEntry condition)
        {
            try
            {
                /*
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
                */
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to match index by thread.", ex);
                throw;
            }
        }

        private static void ExplainLookupConditionRecursive(ref StringBuilder result,
            EngineCore core, IndexingConditionOptimization optimization, IndexingConditionLookup lookup,
            PhysicalSchema physicalSchema, string workingSchemaPrefix, ConditionEntry condition, int attributeDepth)
        {
            var conditionSet = lookup.AttributeConditionSets[lookup.Index.Attributes[attributeDepth].Field.EnsureNotNull()];

            foreach (var singleCondition in conditionSet)
            {
                var indexAttribute = lookup.Index.Attributes[attributeDepth].Field;

                if (attributeDepth == lookup.AttributeConditionSets.Count - 1)
                {
                    //TODO: Reimplement
                    /*
                    if (singleCondition.Right.IsConstant)
                    {
                        result.AppendLine("• " + Pad(1 + attributeDepth) + $"Leaf seek '{indexAttribute}' on constant '{singleCondition.Left.Key}' {singleCondition.LogicalQualifier} '{singleCondition.Right.Value}'.");
                    }
                    else
                    {
                        result.AppendLine("• " + Pad(1 + attributeDepth) + $"Leaf seek '{indexAttribute}' on value of '{singleCondition.Left.Key}' {singleCondition.LogicalQualifier} '{singleCondition.Right.Key}'.");
                    }
                    */
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
