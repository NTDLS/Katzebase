using Katzebase.Engine.Indexes;
using Katzebase.Engine.Schemas;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary;
using System.Text;
using static Katzebase.Engine.KbLib.EngineConstants;
using Katzebase.Engine.Transactions;

namespace Katzebase.Engine.Query.Constraints
{
    internal class ConditionLookupOptimization
    {
        /// <summary>
        /// A list of the indexes that have been selected by the optimizer for the specified conditions.
        /// </summary>
        public List<IndexSelection> IndexSelection { get; set; } = new();

        /// <summary>
        /// A clone of the conditions that this set of index selections was built for. Also contains the indexes associated with each subset of conditions.
        /// </summary>
        public Conditions Conditions { get; private set; }

        public ConditionLookupOptimization(Conditions conditions)
        {
            Conditions = conditions.Clone();
        }
        #region Builder.

        /// <summary>
        /// Takes a nested set of conditions and returns a selection of indexes as well as a clone of the conditions with associated indexes.
        /// </summary>
        /// <returns>A selection of indexes as well as a clone of the conditions with associated indexes</returns>
        public static ConditionLookupOptimization Build(Core core, Transaction transaction, PersistSchema schemaMeta, Conditions conditions)
        {
            //TODO: This does not support multi-schma conditions. Perhaps we make different builders for SSQ and MSQ queries?
            //TODO: This does not support intra-schema join caluses. Perhaps we make different builders for intra-schema join clauses?

            try
            {
                /* This still has condition values in it, that wont work. *Face palm*
                var cacheItem = core.LookupOptimizationCache.Get(conditions.Hash) as ConditionLookupOptimization;
                if (cacheItem != null)
                {
                    return cacheItem;
                }
                */

                var indexCatalog = core.Indexes.GetIndexCatalog(transaction, schemaMeta, LockOperation.Read);

                var lookupOptimization = new ConditionLookupOptimization(conditions);

                foreach (var subset in conditions.Subsets)
                {
                    var potentialIndexs = new List<PotentialIndex>();

                    //Loop though each index in the schema.
                    foreach (var indexMeta in indexCatalog.Collection)
                    {
                        var handledKeyNames = new List<string>();

                        for (int i = 0; i < indexMeta.Attributes.Count; i++)
                        {
                            if (indexMeta.Attributes == null || indexMeta.Attributes[i] == null)
                            {
                                throw new KbNullException($"Value should not be null {nameof(indexMeta.Attributes)}.");
                            }

                            var keyName = indexMeta.Attributes[i].Field?.ToLower();
                            if (keyName == null)
                            {
                                throw new KbNullException($"Value should not be null {nameof(keyName)}.");
                            }

                            if (subset.Conditions.Any(o => o.Left.Value == keyName && !o.CoveredByIndex))
                            {
                                handledKeyNames.Add(keyName);
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (handledKeyNames.Count > 0)
                        {
                            var potentialIndex = new PotentialIndex(indexMeta, handledKeyNames);
                            potentialIndexs.Add(potentialIndex);
                        }
                    }

                    //Grab the index that matches the most of our supplied keys but also has the least attributes.
                    var firstIndex = (from o in potentialIndexs where o.Tried == false select o)
                        .OrderByDescending(s => s.CoveredFields.Count)
                        .ThenBy(t => t.Index.Attributes.Count).FirstOrDefault();
                    if (firstIndex != null)
                    {
                        var handledKeys = (from o in subset.Conditions where firstIndex.CoveredFields.Contains(o.Left.Value ?? string.Empty) select o).ToList();
                        foreach (var handledKey in handledKeys)
                        {
                            handledKey.CoveredByIndex = true;
                        }

                        firstIndex.Tried = true;

                        var indexSelection = new IndexSelection(firstIndex.Index, firstIndex.CoveredFields);

                        lookupOptimization.IndexSelection.Add(indexSelection);

                        //Mark which condition this index selection satisifies.
                        var sourceSubset = lookupOptimization.Conditions.SubsetByKey(subset.SubsetKey);
                        Utility.EnsureNotNull(sourceSubset);
                        sourceSubset.IndexSelection = indexSelection;

                        foreach (var conditon in sourceSubset.Conditions)
                        {
                            if (indexSelection.CoveredFields.Any(o => o == conditon.Left.Value))
                            {
                                conditon.CoveredByIndex = true;
                            }
                        }
                    }
                }

                //core.LookupOptimizationCache.Add(conditions.Hash, lookupOptimization, DateTime.Now.AddMinutes(10));

                return lookupOptimization;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to select indexes for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        public bool CanApplyIndexing(ConditionSubset subset)
        {
            //Currently we can only use a partial index match if all of the conditions in a group are "AND"s,
            //  so if we have an "OR" and any of the conditions are not covered then skip the indexing.
            if (subset.Conditions.Any(o => o.LogicalConnector == LogicalConnector.Or) && subset.Conditions.Any(o => o.CoveredByIndex == false))
            {
                return false;
            }
            return true;
        }

        private bool? _canApplyIndexingResultCached = null;

        public bool CanApplyIndexing()
        {
            if (_canApplyIndexingResultCached != null)
            {
                return (bool)_canApplyIndexingResultCached;
            }

            if (Conditions.NonRootSubsets.Any(o => o.IndexSelection == null) == false)
            {
                //All condition subsets have a selected index. Start building a list of possible document IDs.
                foreach (var subset in Conditions.NonRootSubsets)
                {
                    if (CanApplyIndexing(subset) == false)
                    {
                        _canApplyIndexingResultCached = false;
                        return false;
                    }

                }
                _canApplyIndexingResultCached = true;
                return true;
            }

            _canApplyIndexingResultCached = false;
            return false;
        }

        #region Optimization explanation.
        /*
         * Probably need to redo these, there is a better way to explain whats going on. :)
         */

        string FriendlyExp(string val) => val.ToUpper().Replace("C_", "Condition").Replace("S_", "SubExpression").Replace("||", "OR").Replace("&&", "AND");

        public string BuildFullVirtualExpression()
        {
            if (Conditions.Subsets.Count == 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder();
            result.AppendLine($"[{FriendlyExp(Conditions.RootSubsetKey)}]" + (CanApplyIndexing() ? " {Indexable}" : " {non-Indexable}"));

            if (Conditions.Root.SubsetKeys.Count > 0)
            {
                result.AppendLine("(");

                foreach (var subsetKey in Conditions.Root.SubsetKeys)
                {
                    var subset = Conditions.SubsetByKey(subsetKey);
                    result.AppendLine($"  [{FriendlyExp(subset.Expression)}]" + (CanApplyIndexing(subset) ? " {Indexable (" + subset.IndexSelection?.Index.Name + ")}" : " {non-Indexable}"));

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
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subset = Conditions.SubsetByKey(subsetKey);
                result.AppendLine("".PadLeft((depth) * 4, ' ') + $"[{FriendlyExp(subset.Expression)}]" + (CanApplyIndexing(subset) ? " {Indexable (" + subset.IndexSelection?.Index.Name + ")}" : " {non-Indexable}"));

                if (subset.Conditions.Count > 0)
                {
                    result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + FriendlyExp(subsetKey) + "->" + "(");
                    foreach (var condition in subset.Conditions)
                    {
                        result.AppendLine("".PadLeft((depth + 1) * 4, ' ') + $"{FriendlyExp(condition.ConditionKey)}: {condition.Left} {condition.LogicalQualifier} '{condition.Right}'");
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
                    result.AppendLine("".PadLeft((depth + 1) * 4, ' ') + $"{FriendlyExp(condition.ConditionKey)}: {condition.Left} {condition.LogicalQualifier} '{condition.Right}'");
                }
                result.AppendLine("".PadLeft((depth + 1) * 2, ' ') + ")");
            }
        }

        #endregion
    }
}
