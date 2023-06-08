using Katzebase.Engine.Documents;
using Katzebase.Engine.Indexes;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Newtonsoft.Json.Linq;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Query.Searchers.SingleSchema
{
    internal static class SSQStaticMethods
    {
        /// <summary>
        /// Gets all documents by a subset of conditions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="documentCatalogItems"></param>
        /// <param name="schemaMeta"></param>
        /// <param name="query"></param>
        /// <param name="lookupOptimization"></param>
        /// <returns></returns>
        public static SSQDocumentLookupResults GetSingleSchemaDocumentsByConditions(Core core,
            PerformanceTrace? pt, Transaction transaction, string schemaName, PreparedQuery query)
        {
            //Lock the schema:
            var ptLockSchema = pt?.BeginTrace<PersistSchema>(PerformanceTraceType.Lock);
            var schemaMeta = core.Schemas.VirtualPathToMeta(transaction, schemaName, LockOperation.Read);
            if (schemaMeta == null || schemaMeta.Exists == false)
            {
                throw new KbInvalidSchemaException(schemaName);
            }
            ptLockSchema?.EndTrace();
            Utility.EnsureNotNull(schemaMeta.DiskPath);

            //Lock the document catalog:
            var documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, DocumentCatalogFile);
            var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(pt, transaction, documentCatalogDiskPath, LockOperation.Read);
            Utility.EnsureNotNull(documentCatalog);

            ConditionLookupOptimization? lookupOptimization = null;

            //If we dont have anby conditions then we just need to return all rows from the schema.
            if (query.Conditions.Subsets.Count > 0)
            {
                lookupOptimization = ConditionLookupOptimization.Build(core, transaction, schemaMeta, query.Conditions);

                //Create a reference to the entire document catalog.
                var limitedDocumentCatalogItems = documentCatalog.Collection;

                if (lookupOptimization.CanApplyIndexing())
                {
                    //We are going to create a limited document catalog from the indexes. So kill the reference and create an empty list.
                    limitedDocumentCatalogItems = new List<PersistDocumentCatalogItem>();

                    //All condition subsets have a selected index. Start building a list of possible document IDs.
                    foreach (var subset in lookupOptimization.Conditions.NonRootSubsets)
                    {
                        Utility.EnsureNotNull(subset.IndexSelection);
                        Utility.EnsureNotNull(subset.IndexSelection.Index.DiskPath);

                        var indexPageCatalog = core.IO.GetPBuf<PersistIndexPageCatalog>(pt, transaction, subset.IndexSelection.Index.DiskPath, LockOperation.Read);
                        Utility.EnsureNotNull(indexPageCatalog);

                        var documentIds = core.Indexes.MatchDocuments(pt, indexPageCatalog, subset.IndexSelection, subset);

                        limitedDocumentCatalogItems.AddRange(documentCatalog.Collection.Where(o => documentIds.Contains(o.Id)).ToList());
                    }
                }
                else
                {
                    #region Why no indexing? Find out here!
                    //   * One or more of the conditon subsets lacks an index.
                    //   *
                    //   *   Since indexing requires that we can ensure document elimination we will have
                    //   *      to ensure that we have a covering index on EACH-and-EVERY conditon group.
                    //   *
                    //   *   Then we can search the indexes for each condition group to obtain a list of all possible
                    //   *       document IDs, then use those document IDs to early eliminate documents from the main lookup loop.
                    //   *
                    //   *   If any one conditon group does not have an index, then no indexing will be used at all since all
                    //   *      documents will need to be scaned anyway. To prevent unindexed scans, reduce the number of
                    //   *      condition groups (nested in parentheses).
                    //   *
                    //   * ConditionLookupOptimization:BuildFullVirtualExpression() Will tell you why we cant use an index.
                    //   * var explanationOfIndexability = lookupOptimization.BuildFullVirtualExpression();
                    //*
                    #endregion
                }
            }

            Utility.EnsureNotNull(schemaMeta.DiskPath);

            var ptThreadCreation = pt?.BeginTrace(PerformanceTraceType.ThreadCreation);

            var threadPool = new SSQLookupThreadPool(core, pt, transaction, schemaMeta, query, lookupOptimization);
            int threadCount = Environment.ProcessorCount * 4;
            threadPool.Start(SingleSchemaLookupThreadProc, threadCount > 32 ? 32 : threadCount);

            ptThreadCreation?.EndTrace();

            foreach (var documentCatalogItem in documentCatalog.Collection)
            {
                if (query.RowLimit != 0 && threadPool.Results.Collection.Count >= query.RowLimit)
                {
                    break;
                }

                if (threadPool.HasException)
                {
                    break;
                }

                threadPool.Queue.Enqueue(documentCatalogItem);
            }

            var ptThreadCompletion = pt?.BeginTrace(PerformanceTraceType.ThreadCompletion);
            threadPool.WaitForCompletion();
            ptThreadCompletion?.EndTrace();

            if (query.RowLimit > 0)
            {
                //Multithreading can yeild a few more rows than we need if we have a limiter.
                threadPool.Results.Collection = threadPool.Results.Collection.Take(query.RowLimit).ToList();
            }

            return threadPool.Results;
        }

        internal static void SingleSchemaLookupThreadProc(object? p)
        {
            var param = p as SSQLookupThreadPool;
            Utility.EnsureNotNull(param);

            try
            {
                Utility.EnsureNotNull(param.SchemaMeta);
                Utility.EnsureNotNull(param.SchemaMeta.DiskPath);

                NCalc.Expression? expression = null;

                if (param.LookupOptimization != null)
                {
                    expression = new NCalc.Expression(param.LookupOptimization.Conditions.HighLevelExpressionTree);
                }

                while (param.ContinueToProcessQueue)
                {
                    var documentCatalogItem = param.Queue.Dequeue();
                    if (documentCatalogItem == null)
                    {
                        continue;
                    }

                    var persistDocumentDiskPath = Path.Combine(param.SchemaMeta.DiskPath, documentCatalogItem.FileName);

                    var persistDocument = param.Core.IO.GetJson<PersistDocument>(param.PT, param.Transaction, persistDocumentDiskPath, LockOperation.Read);
                    Utility.EnsureNotNull(persistDocument);
                    Utility.EnsureNotNull(persistDocument.Content);

                    var jContent = JObject.Parse(persistDocument.Content);

                    Utility.EnsureNotNull(documentCatalogItem.Id);

                    if (expression != null && param.LookupOptimization != null)
                    {
                        SetExpressionParameters(ref expression, param.LookupOptimization.Conditions, jContent);
                    }

                    var ptEvaluate = param.PT?.BeginTrace(PerformanceTraceType.Evaluate);
                    bool evaluation = (expression == null) || (bool)expression.Evaluate();
                    ptEvaluate?.EndTrace();

                    if (evaluation)
                    {
                        Utility.EnsureNotNull(persistDocument.Id);
                        var result = new SSQDocumentLookupResult((Guid)persistDocument.Id);

                        foreach (var field in param.Query.SelectFields)
                        {
                            jContent.TryGetValue(field.Key, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken);
                            result.Values.Add(jToken?.ToString() ?? string.Empty);
                        }

                        param.Results.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                param.Exception = ex;
            }
            finally
            {
                param.DecrementRunningThreadCount();
            }
        }

        /// <summary>
        /// Gets the json content values for the specified conditions.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="conditions"></param>
        /// <param name="jContent"></param>
        private static void SetExpressionParameters(ref NCalc.Expression expression, Conditions conditions, JObject jContent)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditions.Root.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetExpressionParametersRecursive(ref expression, conditions, subExpression, jContent);
            }
        }

        private static void SetExpressionParametersRecursive(ref NCalc.Expression expression, Conditions conditions, ConditionSubset conditionSubset, JObject jContent)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetExpressionParametersRecursive(ref expression, conditions, subExpression, jContent);
            }

            foreach (var condition in conditionSubset.Conditions)
            {
                Utility.EnsureNotNull(condition.Left.Value);

                //Get the value of the condition:
                if (!jContent.TryGetValue(condition.Left.Value, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                {
                    throw new KbParserException($"Field not found in document [{condition.Left.Value}].");
                }

                expression.Parameters[condition.ConditionKey] = condition.IsMatch(jToken.ToString().ToLower());
            }
        }
    }
}
