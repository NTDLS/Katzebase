using Katzebase.Engine.Documents;
using Katzebase.Engine.Indexes;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Query.Searchers.SingleSchema.Threading;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Newtonsoft.Json.Linq;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Query.Searchers.SingleSchema.Threading.SSQDocumentThreadingConstants;
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

            var lookupOptimization = SSQStaticOptimization.SelectIndexesForConditionLookupOptimization(core, transaction, schemaMeta, query.Conditions);

            //If we dont have anby conditions then we just need to return all rows from the schema.
            //TODO: Add threading.
            if (query.Conditions.Subsets.Count == 0)
            {
                Utility.EnsureNotNull(schemaMeta.DiskPath);

                var results = new SSQDocumentLookupResults();
                foreach (var documentCatalogItem in documentCatalog.Collection)
                {
                    if (query.RowLimit != 0 && results.Collection.Count >= query.RowLimit)
                    {
                        break;
                    }

                    var persistDocumentDiskPath = Path.Combine(schemaMeta.DiskPath, documentCatalogItem.FileName);

                    var persistDocument = core.IO.GetJson<PersistDocument>(pt, transaction, persistDocumentDiskPath, LockOperation.Read);
                    Utility.EnsureNotNull(persistDocument);
                    Utility.EnsureNotNull(persistDocument.Content);

                    var jContent = JObject.Parse(persistDocument.Content);

                    Utility.EnsureNotNull(documentCatalogItem.Id);
                    var result = new SSQDocumentLookupResult(documentCatalogItem.Id);

                    if (query.SelectFields.Count == 0)
                    {
                        //The one thread will add the rows, so when the other threads are
                        //  unblocked they will see that they have been added and skip adding them.
                        foreach (var child in jContent)
                        {
                            query.SelectFields.Add(new QueryField(child.Key, "", child.Key));
                        }
                    }

                    foreach (var field in query.SelectFields)
                    {
                        if (jContent.TryGetValue(field.Key, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                        {
                            result.Values.Add(jToken.ToString());
                        }
                        else
                        {
                            result.Values.Add(string.Empty);
                        }
                    }

                    results.Add(result);

                }
                return results;
            }

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
                //   * One or more of the conditon subsets lacks an index.
                //   *
                //   *   Since indexing requires that we can ensure document elimination, we will have
                //   *      to ensure that we have a covering index on EACH-and-EVERY conditon group.
                //   *
                //   *   Then we can search the indexes for each condition group to obtain a list of all possible document IDs,
                //   *       then use those document IDs to early eliminate documents from the main lookup loop.
                //   *
                //   *   If any one conditon group does not have an index, then no indexing will be used at all since all documents
                //   *       will need to be scaned anyway. To prevent unindexed scans, reduce the number of condition groups (nested in parentheses).
                //   *
                //   * ConditionLookupOptimization:BuildFullVirtualExpression() Will tell you why we cant use an index.
                //   * var explanationOfIndexability = lookupOptimization.BuildFullVirtualExpression();
                //*
            }

            var ptThreadCreation = pt?.BeginTrace(PerformanceTraceType.ThreadCreation);
            var threads = new SSQDocumentLookupThreads(core, pt, transaction, schemaMeta, query, lookupOptimization, DocumentLookupThreadProc);

            string fistSchemaAlias = query.Schemas.First().Alias;

            int maxThreads = 4;
            if (limitedDocumentCatalogItems.Count > 100)
            {
                maxThreads = Environment.ProcessorCount;
                if (limitedDocumentCatalogItems.Count > 1000)
                {
                    maxThreads = Environment.ProcessorCount * 2;
                }
            }

            threads.InitializePool(maxThreads);

            ptThreadCreation?.EndTrace();

            //Loop through each document in the catalog:
            foreach (var documentCatalogItem in limitedDocumentCatalogItems)
            {
                var ptThreadQueue = pt?.BeginTrace(PerformanceTraceType.ThreadQueue);

                if (query.RowLimit != 0 && threads.Results.Collection.Count >= query.RowLimit)
                {
                    break;
                }

                threads.Enqueue(documentCatalogItem);
                ptThreadQueue?.EndTrace();
            }

            var ptThreadCompletion = pt?.BeginTrace(PerformanceTraceType.ThreadCompletion);
            threads.WaitOnThreadCompletion();
            ptThreadCompletion?.EndTrace();

            var exceptionThreads = threads.Slots.Where(o => o.State == DocumentLookupThreadState.Exception);
            if (exceptionThreads.Any())
            {
                var firstException = exceptionThreads.First();
                if (firstException != null && firstException.Exception != null)
                {
                    throw firstException.Exception;
                }
            }

            var ptThreadCompletion2 = pt?.BeginTrace(PerformanceTraceType.ThreadCompletion);
            threads.Stop();
            ptThreadCompletion2?.EndTrace();

            if (query.RowLimit > 0)
            {
                //Multithreading can yeild a few more rows than we need if we have a limiter.
                threads.Results.Collection = threads.Results.Collection.Take(query.RowLimit).ToList();
            }

            return threads.Results;
        }

        /// <summary>
        /// Thread proc for looking up documents. These are started in a batch per-query and listen for lookup requests.
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="KbParserException"></exception>
        private static void DocumentLookupThreadProc(object? obj)
        {
            Utility.EnsureNotNull(obj);
            var param = (SSQDocumentLookupThreadParam)obj;
            var slot = param.ThreadSlots[param.ThreadSlotNumber];

            try
            {
                var expression = new NCalc.Expression(param.LookupOptimization.Conditions.HighLevelExpressionTree);

                Utility.EnsureNotNull(param.SchemaMeta.DiskPath);

                while (true)
                {
                    slot.State = DocumentLookupThreadState.Ready;

                    var ptThreadReady = param.PT?.BeginTrace(PerformanceTraceType.ThreadReady);
                    slot.Event.WaitOne();
                    ptThreadReady?.EndTrace();

                    if (slot.State == DocumentLookupThreadState.Shutdown)
                    {
                        return;
                    }

                    slot.State = DocumentLookupThreadState.Executing;

                    var persistDocumentDiskPath = Path.Combine(param.SchemaMeta.DiskPath, slot.DocumentCatalogItem.FileName);

                    var persistDocument = param.Core.IO.GetJson<PersistDocument>(param.PT, param.Transaction, persistDocumentDiskPath, LockOperation.Read);
                    Utility.EnsureNotNull(persistDocument);
                    Utility.EnsureNotNull(persistDocument.Content);

                    var jContent = JObject.Parse(persistDocument.Content);

                    SetExpressionParameters(ref expression, param.LookupOptimization.Conditions, jContent);

                    var ptEvaluate = param.PT?.BeginTrace(PerformanceTraceType.Evaluate);
                    bool evaluation = (bool)expression.Evaluate();
                    ptEvaluate?.EndTrace();

                    if (evaluation)
                    {
                        Utility.EnsureNotNull(persistDocument.Id);
                        var result = new SSQDocumentLookupResult((Guid)persistDocument.Id);

                        foreach (var field in param.Query.SelectFields)
                        {
                            if (jContent.TryGetValue(field.Key, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                            {
                                result.Values.Add(jToken.ToString());
                            }
                            else
                            {
                                result.Values.Add(string.Empty);
                            }
                        }

                        param.Results.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                slot.State = DocumentLookupThreadState.Exception;
                slot.Exception = ex;
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
                if (jContent.TryGetValue(condition.Left.Value, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                {
                    expression.Parameters[condition.ConditionKey] = condition.IsMatch(jToken.ToString().ToLower());
                }
                else
                {
                    throw new KbParserException($"Field not found in document [{condition.Left.Value}].");
                }
            }
        }

    }
}
