using Katzebase.Engine.Indexes;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Query.Searchers.SingleSchema
{
    internal static class SSQStaticOptimization
    {
        /// <summary>
        /// Takes a nested set of conditions and returns a selection of indexes as well as a clone of the conditions with associated indexes.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schemaMeta"></param>
        /// <param name="conditions">Nested conditions.</param>
        /// <returns>A selection of indexes as well as a clone of the conditions with associated indexes</returns>
        public static ConditionLookupOptimization SelectIndexesForConditionLookupOptimization(Core core, Transaction transaction, PersistSchema schemaMeta, Conditions conditions)
        {
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
    }
}
