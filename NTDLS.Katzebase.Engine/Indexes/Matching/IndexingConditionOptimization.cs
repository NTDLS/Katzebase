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

        private readonly Transaction _transaction;

        public IndexingConditionOptimization(Transaction transaction, Conditions conditions)
        {
            _transaction = transaction;
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

                        if (compositeIndex != null)
                        {
                            IndexingConditionLookup indexingConditionLookup = new(compositeIndex.Index);

                            //Find the condition fields that match the index attributes.
                            foreach (var attribute in compositeIndex.Index.Attributes)
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
                                }

                                indexingConditionLookup.Conditions.Add(attribute.Field.EnsureNotNull(), matchedConditions);
                            }

                            if (indexingConditionLookup.Conditions.Count > 0)
                            {
                                indexingConditionGroup.Lookups.Add(indexingConditionLookup);
                            }
                        }
                        else
                        {
                            //No suitable composite index was found.
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
                                }

                                indexingConditionLookup.Conditions.Add(attribute.Field.EnsureNotNull(), matchedConditions);
                            }

                            if (indexingConditionLookup.Conditions.Count > 0)
                            {
                                indexingConditionGroup.Lookups.Add(indexingConditionLookup);
                            }
                        }
                        else
                        {
                            //No suitable composite index was found.
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
        /// This function makes a (somewhat) user readable expression tree, used for debugging and explanations.
        /// It includes indexes where they can be applied.
        /// It also demonstrates how we process the recursive condition logic.
        /// </summary>
        public string ExplainOptimization(int indentation = 0)
        {
            var result = new StringBuilder();

            if (Conditions.Root.ExpressionKeys.Count > 0)
            {
                //The root condition is just a pointer to a child condition, so get the "root" child condition.
                var rootCondition = Conditions.SubConditionFromExpressionKey(Conditions.Root.Key);
                ExplainSubCondition(ref result, rootCondition, indentation);
            }

            return result.ToString();
        }

        /// <summary>
        /// This function makes a (somewhat) user readable expression tree, used for debugging and explanations.
        /// It includes indexes where they can be applied.
        /// It also demonstrates how we process the recursive condition logic.
        /// Called by parent ExplainOptimization()
        /// </summary>
        private void ExplainSubCondition(ref StringBuilder result, SubCondition givenSubCondition, int indentation)
        {
            foreach (var expressionKey in givenSubCondition.ExpressionKeys)
            {
                var subCondition = Conditions.SubConditionFromExpressionKey(expressionKey);

                //TODO: definitely not correct!! Just unbreaking the build.
                var indexName = subCondition.IndexSelections.First().EnsureNotNull().Index.Name;

                result.AppendLine(Pad(indentation + 1)
                    + $"{Conditions.FriendlyPlaceholder(subCondition.Key)} is ({Conditions.FriendlyPlaceholder(subCondition.Expression)})"
                    + (indexName != null ? $" [{indexName}]" : ""));

                result.AppendLine(Pad(indentation + 1) + "(");

                if (subCondition.Conditions.Count > 0)
                {
                    foreach (var condition in subCondition.Conditions)
                    {
                        result.AppendLine(Pad(indentation + 2)
                            + $"{Conditions.FriendlyPlaceholder(condition.ConditionKey)} is ({condition.Left} {condition.LogicalQualifier} {condition.Right})");
                    }
                }

                if (subCondition.ExpressionKeys.Count > 0)
                {
                    result.AppendLine(Pad(indentation + 2) + "(");
                    ExplainSubCondition(ref result, subCondition, indentation + 2);
                    result.AppendLine(Pad(indentation + 2) + ")");
                }

                result.AppendLine(Pad(indentation + 1) + ")");
            }
        }

        #endregion
    }
}
