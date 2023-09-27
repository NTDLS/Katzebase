using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Schemas;
using System.Text;
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
        public static ConditionLookupOptimization Build(Core core,
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

                var lookupOptimization = new ConditionLookupOptimization(conditions);

                foreach (var subset in conditions.Subsets)
                {
                    if (subset.Conditions.Where(o => o.Left.Prefix != workingSchemaPrefix).Any())
                    {
                        if (subset.Conditions.Where(o => o.LogicalConnector != LogicalConnector.And).Any() == false)
                        {
                            //We can't yet figure out how to eliminate documents if the conditions are for more
                            //..    than one schema and all of the logical connectors are not AND. This can be done however.
                            //  We just genrally have alot of optimization trouble with ORs.
                            continue;
                        }
                    }

                    var potentialIndexs = new List<PotentialIndex>();

                    //Loop though each index in the schema.
                    foreach (var physicalIindex in indexCatalog.Collection)
                    {
                        var handledKeyNames = new List<PrefixedField>();

                        for (int i = 0; i < physicalIindex.Attributes.Count; i++)
                        {
                            if (physicalIindex.Attributes == null || physicalIindex.Attributes[i] == null)
                            {
                                throw new KbNullException($"Value should not be null {nameof(physicalIindex.Attributes)}.");
                            }

                            var keyName = physicalIindex.Attributes[i].Field?.ToLower();
                            if (keyName == null)
                            {
                                throw new KbNullException($"Value should not be null {nameof(keyName)}.");
                            }

                            var matchedNonCovertedConditions =
                                subset.Conditions.Where(o => o.CoveredByIndex == false
                                    && o.Left.Value == keyName && o.Left.Prefix == workingSchemaPrefix);

                            foreach (var matchedCondition in matchedNonCovertedConditions)
                            {
                                handledKeyNames.Add(PrefixedField.Parse(matchedCondition.Left.Key));
                            }

                            if (matchedNonCovertedConditions.Any() == false)
                            {
                                break;
                            }
                        }

                        if (handledKeyNames.Count > 0)
                        {
                            var potentialIndex = new PotentialIndex(physicalIindex, handledKeyNames);
                            potentialIndexs.Add(potentialIndex);
                        }
                    }

                    List<Condition> GetCovertedConditions(List<Condition> conditions, List<PrefixedField> coveredFields)
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

                    //Grab the index that matches the most of our supplied keys but also has the least attributes.
                    var firstIndex = (from o in potentialIndexs where o.Tried == false select o)
                        .OrderByDescending(s => s.CoveredFields.Count)
                        .ThenBy(t => t.Index.Attributes.Count).FirstOrDefault();
                    if (firstIndex != null)
                    {
                        var handledKeys = GetCovertedConditions(subset.Conditions, firstIndex.CoveredFields);

                        //Where the left value is in the covered fields:

                        //var handledKeys = (from o in subset.Conditions where firstIndex.CoveredFields.Contains(o.Left.Value ?? string.Empty) select o).ToList();
                        foreach (var handledKey in handledKeys)
                        {
                            handledKey.CoveredByIndex = true;
                        }

                        firstIndex.SetTried();

                        var indexSelection = new IndexSelection(firstIndex.Index, firstIndex.CoveredFields);

                        lookupOptimization.IndexSelection.Add(indexSelection);

                        //Mark which condition this index selection satisifies.
                        var sourceSubset = lookupOptimization.Conditions.SubsetByKey(subset.SubsetKey);
                        KbUtility.EnsureNotNull(sourceSubset);
                        sourceSubset.IndexSelection = indexSelection;

                        foreach (var conditon in sourceSubset.Conditions)
                        {
                            if (indexSelection.CoveredFields.Any(o => o.Key == conditon.Left.Key))
                            {
                                conditon.CoveredByIndex = true;
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
                    result.AppendLine($"  [{FriendlyExp(subset.Expression)}]" + (CanApplyIndexing(subset) ? " {Indexable (" + subset.IndexSelection?.PhysicalIndex.Name + ")}" : " {non-Indexable}"));

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
                result.AppendLine("".PadLeft(depth * 4, ' ') + $"[{FriendlyExp(subset.Expression)}]" + (CanApplyIndexing(subset) ? " {Indexable (" + subset.IndexSelection?.PhysicalIndex.Name + ")}" : " {non-Indexable}"));

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
