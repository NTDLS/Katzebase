using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Schemas;
using System.Text;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Query.Constraints
{
    internal class ConditionLookupOptimization
    {
        /// <summary>
        /// A list of the indexes that have been selected by the optimizer for the specified conditions.
        /// </summary>
        public List<IndexSelection> IndexSelection { get; private set; } = new();

        /// <summary>
        /// A clone of the conditions that this set of index selections was built for.
        /// Also contains the indexes associated with each subset of conditions.
        /// </summary>
        public Conditions Conditions { get; private set; }

        private readonly Transaction _transaction;

        public ConditionLookupOptimization(Transaction transaction, Conditions conditions)
        {
            _transaction = transaction;
            Conditions = conditions.Clone();
        }

        #region Builder.

        /// <summary>
        /// Takes a nested set of conditions and returns a selection of indexes as well as a clone of the conditions with associated indexes.
        /// </summary>
        /// <returns>A selection of indexes as well as a clone of the conditions with associated indexes</returns>
        public static ConditionLookupOptimization Build(EngineCore core,
            Transaction transaction, PhysicalSchema physicalSchema, Conditions conditions, string workingSchemaPrefix)
        {
            try
            {
                /* This still has condition values in it, that wont work. *Face palm*
                var cacheItem = core.LookupOptimizationCache.Get(conditions.Hash) as MSQConditionLookupOptimization;
                if (cacheItem != null)
                {
                    return cacheItem;
                }
                */

                var indexCatalog = core.Indexes.AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                var lookupOptimization = new ConditionLookupOptimization(transaction, conditions);

                foreach (var subset in conditions.Subsets)
                {
                    if (subset.Conditions.Any(o => o.Left.Prefix != workingSchemaPrefix))
                    {
                        if (subset.Conditions.Any(o => o.LogicalConnector != LogicalConnector.And) == false)
                        {
                            //We can't yet figure out how to eliminate documents if the conditions are for more
                            //..    than one schema and all of the logical connectors are not AND. This can be done however.
                            //  We just generally have a lot of optimization trouble with ORs.
                            continue;
                        }
                    }

                    var potentialIndexes = new List<PotentialIndex>();

                    //Loop though each index in the schema.
                    foreach (var physicalIndex in indexCatalog.Collection)
                    {
                        var handledKeyNames = new List<PrefixedField>();

                        for (int i = 0; i < physicalIndex.Attributes.Count; i++)
                        {
                            if (physicalIndex.Attributes == null || physicalIndex.Attributes[i] == null)
                            {
                                throw new KbNullException($"Value should not be null {nameof(physicalIndex.Attributes)}.");
                            }

                            var keyName = physicalIndex.Attributes[i].Field?.ToLowerInvariant();
                            if (keyName == null)
                            {
                                throw new KbNullException($"Value should not be null {nameof(keyName)}.");
                            }

                            var matchedNonConvertedConditions =
                                subset.Conditions.Where(o => o.CoveredByIndex == false
                                    && o.Left.Value == keyName && o.Left.Prefix == workingSchemaPrefix);

                            foreach (var matchedCondition in matchedNonConvertedConditions)
                            {
                                handledKeyNames.Add(PrefixedField.Parse(matchedCondition.Left.Key));
                            }

                            if (matchedNonConvertedConditions.Any() == false)
                            {
                                break;
                            }
                        }

                        if (handledKeyNames.Count > 0)
                        {
                            var potentialIndex = new PotentialIndex(physicalIndex, handledKeyNames);
                            potentialIndexes.Add(potentialIndex);
                        }
                    }

                    //Grab the index that matches the most of our supplied keys but also has the least attributes.
                    var firstIndex = (from o in potentialIndexes where o.Tried == false select o)
                        .OrderByDescending(s => s.CoveredFields.Count)
                        .ThenBy(t => t.Index.Attributes.Count).FirstOrDefault();

                    if (firstIndex != null)
                    {
                        var handledKeys = GetConvertedConditions(subset.Conditions, firstIndex.CoveredFields);

                        //Where the left value is in the covered fields:

                        //var handledKeys = (from o in subset.Conditions where firstIndex.CoveredFields.Contains(o.Left.Value ?? string.Empty) select o).ToList();
                        foreach (var handledKey in handledKeys)
                        {
                            handledKey.CoveredByIndex = true;
                        }

                        firstIndex.SetTried();

                        var indexSelection = new IndexSelection(firstIndex.Index, firstIndex.CoveredFields);

                        lookupOptimization.IndexSelection.Add(indexSelection);

                        //Mark which condition this index selection satisfies.
                        var sourceSubset = lookupOptimization.Conditions.SubsetByKey(subset.SubsetKey);
                        sourceSubset.IndexSelection = indexSelection;

                        foreach (var condition in sourceSubset.Conditions)
                        {
                            if (indexSelection.CoveredFields.Any(o => o.Key == condition.Left.Key))
                            {
                                condition.CoveredByIndex = true;
                            }
                        }
                    }
                }

                //core.LookupOptimizationCache.Add(conditions.Hash, lookupOptimization, DateTime.Now.AddMinutes(10));

                //When we get here, we have one index that seems to want to cover multiple tables - no cool man. Not cool.

                return lookupOptimization;
            }
            catch (Exception ex)
            {
                Interactions.Management.LogManager.Error($"Failed to select indexes for process {transaction.ProcessId}.", ex);
                throw;
            }
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

        public static bool CanApplyIndexing(ConditionSubset subset)
        {
            //Currently we can only use a partial index match if all of the conditions in a group are "AND"s,
            //  so if we have an "OR" and any of the conditions are not covered then skip the indexing.
            if (subset.Conditions.Any(o => o.LogicalConnector == LogicalConnector.Or)
                && subset.Conditions.Any(o => o.CoveredByIndex == false))
            {
                return false;
            }
            return true;
        }

        private bool? _canApplyIndexingResultCached = null;

        public bool CanApplyIndexing(bool explain = false)
        {
            if (_canApplyIndexingResultCached != null)
            {
                return (bool)_canApplyIndexingResultCached;
            }

            if (Conditions.NonRootSubsets.Any(o => o.IndexSelection == null) == false)
            {
                //All condition subsets have a selected index.
                foreach (var subset in Conditions.NonRootSubsets)
                {
                    if (CanApplyIndexing(subset) == false)
                    {
                        if (explain)
                        {
                            _transaction.AddMessage($"Indexing invalidated by subset expression: {subset.Expression}.", KbMessageType.Verbose);
                        }
                        _canApplyIndexingResultCached = false;
                        return false;
                    }
                }

                #region Index usage reporting.

                if (explain)
                {
                    var message = new StringBuilder();

                    var friendlyExpression = new StringBuilder();
                    BuildFullVirtualExpression(ref friendlyExpression, Conditions.Root, 1);

                    message.AppendLine($"Expression: ({friendlyExpression}) {{");

                    message.AppendLine($"Applying {IndexSelection.Count} index(s).");

                    foreach (var index in IndexSelection)
                    {
                        var coveredFields = string.Join("', '", index.CoveredFields.Select(o => o.Key)).Trim();
                        message.AppendLine($"Index '{index.PhysicalIndex.Name}' covers {coveredFields}");
                    }

                    //All condition subsets have a selected index. Start building a list of possible document IDs.
                    foreach (var subset in Conditions.NonRootSubsets)
                    {
                        message.AppendLine($"Expression: ({subset.Expression}) {{");

                        foreach (var condition in subset.Conditions)
                        {
                            string leftIndex = string.Empty;
                            string rightIndex = string.Empty;

                            if (condition.CoveredByIndex)
                            {
                                foreach (var index in IndexSelection)
                                {
                                    if (index.CoveredFields.Any(o => o.Key == condition.Left.Key))
                                    {
                                        leftIndex = index.PhysicalIndex.Name;
                                    }
                                    if (index.CoveredFields.Any(o => o.Key == condition.Right.Key))
                                    {
                                        rightIndex = index.PhysicalIndex.Name;
                                    }
                                }
                            }

                            string leftValue = condition.Left.IsConstant ? $"'{condition.Left.Key}'" : condition.Left.Key;
                            string rightValue = condition.Right.IsConstant ? $"'{condition.Right.Key}'" : condition.Right.Key;

                            string indexInfo = string.Empty;

                            if (string.IsNullOrEmpty(leftIndex) == false || string.IsNullOrEmpty(rightIndex) == false)
                            {
                                indexInfo += ", Indexes (";

                                if (string.IsNullOrEmpty(leftIndex) == false)
                                {
                                    indexInfo += $"Left: [{leftIndex}] ";
                                }

                                if (string.IsNullOrEmpty(rightIndex) == false)
                                {
                                    indexInfo += $"Right: [{rightIndex}] ";
                                }

                                indexInfo = indexInfo.Trim();

                                indexInfo += ")";
                            }

                            message.AppendLine($"\t'{condition.ConditionKey}: ({leftValue} {condition.LogicalQualifier} {rightValue}){indexInfo}");
                        }
                        message.AppendLine("}");
                    }

                    _transaction.AddMessage(message.ToString(), KbMessageType.Verbose);
                }

                #endregion

                _canApplyIndexingResultCached = true;
                return true;
            }

            if (explain)
            {
                _transaction.AddMessage($"Indexing invalidated by root expression: {Conditions.Root.Expression}.", KbMessageType.Verbose);
            }

            _canApplyIndexingResultCached = false;
            return false;
        }

        #region Optimization explanation.
        /*
         * Probably need to redo these, there is a better way to explain what's going on. :)
         */

        static string FriendlyExpression(string val) => val.ToUpper()
            .Replace("C_", "Condition")
            .Replace("S_", "SubExpression")
            .Replace("||", "OR")
            .Replace("&&", "AND");

        public string BuildFullVirtualExpression()
        {
            if (Conditions.Subsets.Count == 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder();
            result.AppendLine($"[{FriendlyExpression(Conditions.RootSubsetKey)}]"
                + (CanApplyIndexing() ? " {Indexable}" : " {non-Indexable}"));

            if (Conditions.Root.SubsetKeys.Count > 0)
            {
                result.AppendLine("(");

                foreach (var subsetKey in Conditions.Root.SubsetKeys)
                {
                    var subset = Conditions.SubsetByKey(subsetKey);
                    result.AppendLine($"  [{FriendlyExpression(subset.Expression)}]"
                        + (CanApplyIndexing(subset) ? " {Indexable (" + subset.IndexSelection?.PhysicalIndex.Name + ")}" : " {non-Indexable}"));

                    result.AppendLine("  (");
                    BuildFullVirtualExpression(ref result, subset, 1);
                    result.AppendLine("  )");
                }

                result.AppendLine(")");
            }

            return result.ToString();
        }

        private void BuildFullVirtualExpression(ref StringBuilder result, ConditionSubset conditionSubset, int depth)
        {
            //If we have subsets, then we need to satisfy those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subset = Conditions.SubsetByKey(subsetKey);
                result.AppendLine("".PadLeft(depth * 4, ' ')
                    + $"[{FriendlyExpression(subset.Expression)}]" + (CanApplyIndexing(subset) ? " {Indexable (" + subset.IndexSelection?.PhysicalIndex.Name + ")}" : " {non-Indexable}"));

                if (subset.Conditions.Count > 0)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + FriendlyExpression(subsetKey) + "->" + "(");
                    foreach (var condition in subset.Conditions)
                    {
                        result.AppendLine("".PadLeft((depth + 1) * 4, ' ')
                            + $"{FriendlyExpression(condition.ConditionKey)}: {condition.Left} {condition.LogicalQualifier} '{condition.Right}'");
                    }
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + ")");
                }

                if (subset.SubsetKeys.Count > 0)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + "(");
                    BuildFullVirtualExpression(ref result, subset, depth + 1);
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + ")");
                }
            }

            if (conditionSubset.Conditions.Count > 0)
            {
                result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + "(");
                foreach (var condition in conditionSubset.Conditions)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 4, ' ') + $"{FriendlyExpression(condition.ConditionKey)}: {condition.Left} {condition.LogicalQualifier} '{condition.Right}'");
                }
                result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + ")");
            }
        }

        #endregion
    }
}
