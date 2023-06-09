﻿using Katzebase.Engine.Documents;
using Katzebase.Engine.Indexes;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PrivateLibrary;
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

            //If we dont have any conditions then we just need to return all rows from the schema.
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

            //TODO: We should put some intelligence behind the thread count and queue size.
            int threadCount = Environment.ProcessorCount * 4 > 32 ? 32 : Environment.ProcessorCount * 4;

            var ptThreadCreation = pt?.BeginTrace(PerformanceTraceType.ThreadCreation);
            var param = new LookupThreadParam(core, pt, transaction, schemaMeta, query, lookupOptimization);
            var threadPool = ThreadPoolQueue<PersistDocumentCatalogItem, LookupThreadParam>.CreateAndStart(LookupThreadProc, param, threadCount);
            ptThreadCreation?.EndTrace();

            foreach (var documentCatalogItem in documentCatalog.Collection)
            {
                if (query.RowLimit != 0 && param.Results.Collection.Count >= query.RowLimit)
                {
                    break;
                }

                if (threadPool.HasException)
                {
                    break;
                }

                threadPool.EnqueueWorkItem(documentCatalogItem);
            }

            var ptThreadCompletion = pt?.BeginTrace(PerformanceTraceType.ThreadCompletion);
            threadPool.WaitForCompletion();
            ptThreadCompletion?.EndTrace();

            if (query.RowLimit > 0)
            {
                //Multithreading can yeild a few more rows than we need if we have a limiter.
                param.Results.Collection = param.Results.Collection.Take(query.RowLimit).ToList();
            }

            return param.Results;
        }

        internal class LookupThreadParam
        {
            public SSQDocumentLookupResults Results = new();
            public PersistSchema SchemaMeta { get; private set; }
            public Core Core { get; private set; }
            public PerformanceTrace? PT { get; private set; }
            public Transaction Transaction { get; private set; }
            public PreparedQuery Query { get; private set; }
            public ConditionLookupOptimization? LookupOptimization { get; private set; }

            public LookupThreadParam(Core core, PerformanceTrace? pt, Transaction transaction,
                PersistSchema schemaMeta, PreparedQuery query, ConditionLookupOptimization? lookupOptimization)
            {
                this.Core = core;
                this.PT = pt;
                this.Transaction = transaction;
                this.SchemaMeta = schemaMeta;
                this.Query = query;
                this.LookupOptimization = lookupOptimization;
            }
        }

        internal static void LookupThreadProc(ThreadPoolQueue<PersistDocumentCatalogItem, LookupThreadParam> pool, LookupThreadParam? param)
        {
            Utility.EnsureNotNull(param);

            Utility.EnsureNotNull(param.SchemaMeta);
            Utility.EnsureNotNull(param.SchemaMeta.DiskPath);

            NCalc.Expression? expression = null;

            if (param.LookupOptimization != null)
            {
                expression = new NCalc.Expression(param.LookupOptimization.Conditions.HighLevelExpressionTree);
            }

            while (pool.ContinueToProcessQueue)
            {
                var documentCatalogItem = pool.DequeueWorkItem();
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