﻿using NTDLS.Helpers;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Expressions;
using NTDLS.Katzebase.Parsers;
using NTDLS.Katzebase.Parsers.Conditions;
using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.Parsing;
using NTDLS.Katzebase.PersistentTypes.Index;
using NTDLS.Katzebase.PersistentTypes.Schema;
using System.Text;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Indexes
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
        public static IndexingConditionOptimization SelectUsableIndexes(EngineCore core, Transaction transaction, PreparedQuery query,
            PhysicalSchema physicalSchema, ConditionCollection conditions, string workingSchemaPrefix)
        {
            var optimization = new IndexingConditionOptimization(transaction, conditions);

            var indexCatalog = core.Indexes.AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

            if (WalkConditionTree(optimization, query, transaction, indexCatalog, workingSchemaPrefix))
            {
                return optimization;
            }

            //Invalidate indexing optimization.
            return new IndexingConditionOptimization(transaction, conditions);
        }

        /// <summary>
        /// Takes a nested set of conditions and returns a clone of the conditions with associated selection of indexes.
        /// </summary>
        private static bool WalkConditionTree(IndexingConditionOptimization optimization, PreparedQuery query,
            Transaction transaction, PhysicalIndexCatalog indexCatalog, string workingSchemaPrefix)
        {
            //We only flatten the condition groups because its easer to work with them this way,
            //    these are references to the optimizers copy of the nested groups, so changes
            //    made here affect the condition groups/entries (such as assigning indexes).
            var flattenedGroups = optimization.Conditions.FlattenToGroups();

            foreach (var flattenedGroup in flattenedGroups)
            {
                #region Build list of usable indexes.

                if (flattenedGroup.Collection.OfType<ConditionEntry>().Where(o => o.Left.SchemaAlias.Is(workingSchemaPrefix)).Any() == false)
                {
                    //Each "OR" condition group must have at least one potential indexable match for the selected schema,
                    //  this is because we need to be able to create a full list of all possible documents for this schema,
                    //  and if we have an "OR" group that does not further limit these documents by the given schema then
                    //  we will have to do a full namespace scan anyway.
                    if (flattenedGroup.LogicalConnector == LogicalConnector.Or)
                    {
                        //Invalidate indexing optimization.
                        foreach (var invalidateFlattenedGroup in flattenedGroups)
                        {
                            optimization.IndexingConditionGroup.Clear();
                            invalidateFlattenedGroup.UsableIndexes.Clear();
                            invalidateFlattenedGroup.IndexLookup = null;
                        }

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

                        var applicableConditions = new List<ConditionEntry>();

                        if (string.IsNullOrEmpty(workingSchemaPrefix))
                        {
                            //For non-schema joins, we do not currently support indexing on anything other than constant expressions.
                            //However, I think this could be implemented pretty easily.
                            applicableConditions.AddRange(
                                flattenedGroup.Collection.OfType<ConditionEntry>()
                                .Where(o => o.Left.SchemaAlias.Is(workingSchemaPrefix) && StaticParserField.IsConstantExpression(o.Right.Value)));
                        }
                        else
                        {
                            //For schema joins, we already have a schema document value cache in the lookup functions so we allow non-constants.
                            applicableConditions.AddRange(
                                flattenedGroup.Collection.OfType<ConditionEntry>()
                                .Where(o => o.Left.SchemaAlias.Is(workingSchemaPrefix)));
                        }

                        foreach (var condition in applicableConditions)
                        {
                            if (condition.Left is QueryFieldDocumentIdentifier leftValue && leftValue.FieldName?.Is(attribute.Field) == true)
                            {
                                indexSelection.CoveredConditions.Add(condition);
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
                        indexSelection.IsFullIndexMatch = indexSelection.CoveredConditions
                            .Select(o => o.Left.Value).Distinct().Count() == indexSelection.PhysicalIndex.Attributes.Count;

                        flattenedGroup.UsableIndexes.Add(indexSelection);
                    }
                }

                if (locatedGroupIndex == false)
                {
                    //This group has no indexing, but since it does reference the
                    //  given schema, we are going to have to do a full schema scan.
                    return false; //Invalidate indexing optimization.
                }

                foreach (var condition in flattenedGroup.Collection.OfType<ConditionEntry>())
                {
                    if (condition.Left is QueryFieldDocumentIdentifier && StaticParserField.IsConstantExpression(condition.Right.Value))
                    {
                        //To save time while indexing, we are going to collapse the value here if the expression is a constant.
                        var constantValue = condition.Right.CollapseScalarQueryField(transaction, query, query.SelectFields, new())?.ToLowerInvariant();
                        //TODO: Think about the nullability of constantValue.
                        condition.Right = new QueryFieldCollapsedValue(condition.Right.ScriptLine, constantValue);
                    }

                    //This Works to collapse the value, but we only index on right hand values... so its commented out.
                    /*
                    if (condition.Right is QueryFieldDocumentIdentifier && StaticParserField.IsConstantExpression(condition.Left.Value))
                    {
                        //To save time while indexing, we are going to collapse the value here if the expression is a constant.
                        var constantValue = condition.Left.CollapseScalarQueryField(transaction, query, query.SelectFields, new())?.ToLowerInvariant();
                        //TODO: Think about the nullability of constantValue.
                        condition.Left = new QueryFieldCollapsedValue(condition.Left.ScriptLine, constantValue);
                    }
                    */
                }

                #endregion

                #region Select the best indexes from the usable indexes.

                if (flattenedGroup.UsableIndexes.Count > 0)
                {
                    var indexingConditionGroup = new IndexingConditionGroup(flattenedGroup.LogicalConnector);

                    var conditionsWithApplicableLeftDocumentIdentifiers = flattenedGroup.ThisLevelWithLeftDocumentIdentifiers();

                    //Here we are ordering by the "full match" indexes first, then descending by the number of index attributes.
                    //The thinking being that we want to prefer full index matches that contain multiple index attributes
                    //  (composite indexes), then we want to look at non-composite full matches, and finally let any partial
                    //  match composite indexes pick up any remaining un-optimized conditions, but we want those to be ordered
                    //  ascending by the index attribute account because we want the smallest non-full-match composite indexes
                    //  because they require less work to distill later.

                    var preferenceOrderedIndexSelections = new List<IndexSelection>();

                    var fullMatches = flattenedGroup.UsableIndexes
                        .Where(o => o.IsFullIndexMatch == true && o.CoveredConditions.Any(c => c.IsIndexOptimized == false))
                        .OrderByDescending(o => o.CoveredConditions.Count).ToList();
                    preferenceOrderedIndexSelections.AddRange(fullMatches);

                    var partialMatches = flattenedGroup.UsableIndexes
                        .Where(o => o.IsFullIndexMatch == false && o.CoveredConditions.Any(c => c.IsIndexOptimized == false))
                        .OrderByDescending(o => o.CoveredConditions.Count).ToList();
                    preferenceOrderedIndexSelections.AddRange(partialMatches);

                    foreach (var indexSelection in preferenceOrderedIndexSelections)
                    {
                        var indexingConditionLookup = new IndexingConditionLookup(indexSelection);

                        //Find the condition fields that match the index attributes.
                        foreach (var attribute in indexSelection.PhysicalIndex.Attributes)
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

                            flattenedGroup.IndexLookup = indexingConditionLookup;
                        }
                    }

                    if (indexingConditionGroup.Lookups.Count > 0)
                    {
                        optimization.IndexingConditionGroup.Add(indexingConditionGroup);
                    }
                }

                #endregion
            }

            return true;
        }

        #endregion

        #region Optimization explanation.

        /// <summary>
        /// This function makes returns a string that represents how and where indexes are used to satisfy a query.
        /// </summary>
        public static string ExplainPlan(PhysicalSchema physicalSchema, IndexingConditionOptimization optimization, PreparedQuery query, string workingSchemaPrefix)
        {
            var result = new StringBuilder();

            string schemaIdentifier = $"Schema: [{physicalSchema.Name}]";
            if (!string.IsNullOrEmpty(workingSchemaPrefix))
            {
                schemaIdentifier += $", alias: [{workingSchemaPrefix}]";
            }

            result.AppendLine("<BEGIN>••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••");
            result.AppendLine($"• " + $"{schemaIdentifier}");
            result.AppendLine("•••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••");

            foreach (var group in optimization.Conditions.Collection.OfType<ConditionGroup>())
            {
                ExplainPlanRecursive(physicalSchema, optimization, workingSchemaPrefix, group, query, result);
            }

            result.AppendLine("<END>••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••••");

            return result.ToString();
        }

        private static void ExplainPlanRecursive(PhysicalSchema physicalSchema, IndexingConditionOptimization optimization,
            string workingSchemaPrefix, ConditionGroup givenConditionGroup, PreparedQuery query, StringBuilder result)
        {
            if (givenConditionGroup.IndexLookup != null)
            {
                if (givenConditionGroup.IndexLookup.IndexSelection.IsFullIndexMatch)
                {
                    if (givenConditionGroup.IndexLookup.IndexSelection.PhysicalIndex.Attributes.Count > 1)
                    {
                        result.AppendLine($"Composite index seek [{givenConditionGroup.IndexLookup.IndexSelection.PhysicalIndex.Name}].");
                    }
                    else result.AppendLine($"Index seek [{givenConditionGroup.IndexLookup.IndexSelection.PhysicalIndex.Name}].");
                }
                else
                {
                    if (givenConditionGroup.IndexLookup.IndexSelection.PhysicalIndex.Attributes.Count > 1)
                    {
                        result.AppendLine($"Composite index scan [{givenConditionGroup.IndexLookup.IndexSelection.PhysicalIndex.Name}].");
                    }
                    result.AppendLine($"Index scan [{givenConditionGroup.IndexLookup.IndexSelection.PhysicalIndex.Name}].");
                }

                if (givenConditionGroup.LogicalConnector == LogicalConnector.Or)
                {
                    result.AppendLine("Indexing union operation.");
                }
                else // LogicalConnector.And || LogicalConnector.None
                {
                    result.AppendLine("Indexing intersect operation.");
                }

                foreach (var group in givenConditionGroup.Collection.OfType<ConditionGroup>().Where(o => o.IndexLookup != null))
                {
                    ExplainPlanRecursive(physicalSchema, optimization, workingSchemaPrefix, group, query, result);
                }
            }
            else
            {
                foreach (var entry in givenConditionGroup.Collection.OfType<ConditionEntry>())
                {
                    if (entry.Left is QueryFieldDocumentIdentifier documentIdentifier)
                    {
                        result.AppendLine($"Schema scan of [{documentIdentifier.SchemaAlias}].");
                    }
                }
            }
        }

        #endregion
    }
}
