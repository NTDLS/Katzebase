using Katzebase.Engine.Documents.Threading;
using Katzebase.Engine.Indexes;
using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Query.Tokenizers;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using static Katzebase.Engine.Documents.Threading.DocumentThreadingConstants;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Documents
{
    public class DocumentManager
    {
        private Core core;

        public DocumentManager(Core core)
        {
            this.core = core;
        }

        public KbQueryResult ExecuteExplain(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbQueryResult();
                PerformanceTrace? pt = null;

                var session = core.Sessions.ByProcessId(processId);
                if (session.TraceWaitTimesEnabled)
                {
                    pt = new PerformanceTrace();
                }

                var ptAcquireTransaction = pt?.BeginTrace(PerformanceTraceType.AcquireTransaction);
                using (var txRef = core.Transactions.Begin(processId))
                {
                    ptAcquireTransaction?.EndTrace();

                    var ptLockSchema = pt?.BeginTrace<PersistSchema>(PerformanceTraceType.Lock);
                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, preparedQuery.Schemas[0].Alias, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(preparedQuery.Schemas[0].Alias);
                    }
                    ptLockSchema?.EndTrace();
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    var lookupOptimization = core.Indexes.SelectIndexesForConditionLookupOptimization(txRef.Transaction, schemaMeta, preparedQuery.Conditions);
                    result.Explanation = lookupOptimization.BuildFullVirtualExpression();

                    txRef.Commit(); //Not that we did any work.
                }

                if (session.TraceWaitTimesEnabled && pt != null)
                {
                    foreach (var wt in pt.Aggregations)
                    {
                        result.WaitTimes.Add(new KbNameValue<double>(wt.Key, wt.Value));
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }

        public KbQueryResult ExecuteSelect(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbQueryResult();
                PerformanceTrace? pt = null;

                var session = core.Sessions.ByProcessId(processId);
                if (session.TraceWaitTimesEnabled)
                {
                    pt = new PerformanceTrace();
                }

                var ptAcquireTransaction = pt?.BeginTrace(PerformanceTraceType.AcquireTransaction);
                using (var txRef = core.Transactions.Begin(processId))
                {
                    ptAcquireTransaction?.EndTrace();
                    result = FindDocumentsByPreparedQuery(pt, txRef.Transaction, preparedQuery);
                    txRef.Commit();
                }

                if (session.TraceWaitTimesEnabled && pt != null)
                {
                    foreach (var wt in pt.Aggregations)
                    {
                        result.WaitTimes.Add(new KbNameValue<double>(wt.Key, wt.Value));
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }

        public KbActionResponse ExecuteDelete(ulong processId, PreparedQuery preparedQuery)
        {
            //TODO: This is a stub, this does NOT work.
            try
            {
                var result = new KbActionResponse();
                var pt = new PerformanceTrace();

                var ptAcquireTransaction = pt?.BeginTrace(PerformanceTraceType.AcquireTransaction);
                using (var txRef = core.Transactions.Begin(processId))
                {
                    ptAcquireTransaction?.EndTrace();

                    result = FindDocumentsByPreparedQuery(pt, txRef.Transaction, preparedQuery);

                    //TODO: Delete the documents.

                    /*
                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(txRef.Transaction, documentCatalogDiskPath, LockOperation.Write);

                    var persistDocument = documentCatalog.GetById(newId);
                    if (persistDocument != null)
                    {
                        string documentDiskPath = Path.Combine(schemaMeta.DiskPath, Helpers.GetDocumentModFilePath(persistDocument.Id));

                        core.IO.DeleteFile(txRef.Transaction, documentDiskPath);

                        documentCatalog.Remove(persistDocument);

                        core.Indexes.DeleteDocumentFromIndexes(txRef.Transaction, schemaMeta, persistDocument.Id);

                        core.IO.PutJson(txRef.Transaction, documentCatalogDiskPath, documentCatalog);
                    }
                    */

                    txRef.Commit();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteDelete for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets all documents by a subset of conditions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="documentCatalogItems"></param>
        /// <param name="schemaMeta"></param>
        /// <param name="query"></param>
        /// <param name="lookupOptimization"></param>
        /// <returns></returns>
        private DocumentLookupResults GetAllDocumentsByConditions(PerformanceTrace? pt, Transaction transaction,
            QuerySchemaMap schemaMap, PreparedQuery query, ConditionLookupOptimization lookupOptimization)
        {
            if (query.Conditions.Subsets.Count == 0)
            {
                /*
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    var results = new DocumentLookupResults();
                    foreach (var documentCatalogItem in documentCatalogItems)
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
                        var result = new DocumentLookupResult((Guid)documentCatalogItem.Id);

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
                */
            }

            /*
            //Create a reference to the entire document catalog.
            var limitedDocumentCatalogItems = documentCatalogItems;

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

                    limitedDocumentCatalogItems.AddRange(documentCatalogItems.Where(o => documentIds.Contains(o.Id)).ToList());
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
            */

            var ptThreadCreation = pt?.BeginTrace(PerformanceTraceType.ThreadCreation);
            var threads = new DocumentLookupThreads(pt, transaction, schemaMap, query, lookupOptimization, DocumentLookupThreadProc);

            string fistSchemaAlias = query.Schemas.First().Alias;

            var limitedDocumentCatalogItems = schemaMap[fistSchemaAlias].DocuemntCatalog.Collection;

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
        private void DocumentLookupThreadProc(object? obj)
        {
            Utility.EnsureNotNull(obj);
            var param = (DocumentLookupThreadParam)obj;
            var slot = param.ThreadSlots[param.ThreadSlotNumber];

            try
            {
                string firstSchemaAlias = param.Query.Schemas.First().Alias;
                var firstSchema = param.SchemaMap[firstSchemaAlias].SchemaMeta;

                //param.SchemaMap

                var expression = new NCalc.Expression(param.LookupOptimization.Conditions.Root.Expression);

                Utility.EnsureNotNull(firstSchema.DiskPath);

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

                    var persistDocumentDiskPath = Path.Combine(firstSchema.DiskPath, slot.DocumentCatalogItem.FileName);

                    var persistDocument = core.IO.GetJson<PersistDocument>(param.PT, param.Transaction, persistDocumentDiskPath, LockOperation.Read);
                    Utility.EnsureNotNull(persistDocument);
                    Utility.EnsureNotNull(persistDocument.Content);

                    var jContent = JObject.Parse(persistDocument.Content);

                    //If we have subsets, then we need to satisify those in order to complete the equation.
                    foreach (var subsetKey in param.LookupOptimization.Conditions.Root.SubsetKeys)
                    {
                        var subExpression = param.LookupOptimization.Conditions.SubsetByKey(subsetKey);
                        bool subExpressionResult = SatisifySubExpression(param.PT, param.LookupOptimization, jContent, subExpression);
                        expression.Parameters[subsetKey] = subExpressionResult;
                    }

                    var ptEvaluate = param.PT?.BeginTrace(PerformanceTraceType.Evaluate);
                    bool evaluation = (bool)expression.Evaluate();
                    ptEvaluate?.EndTrace();

                    if (evaluation)
                    {
                        Utility.EnsureNotNull(persistDocument.Id);
                        var result = new DocumentLookupResult((Guid)persistDocument.Id);

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
        /// Mathematically collapses all subexpressions to return a boolean match.
        /// </summary>
        /// <param name="lookupOptimization"></param>
        /// <param name="jContent"></param>
        /// <param name="conditionSubset"></param>
        /// <returns></returns>
        /// <exception cref="KbParserException"></exception>
        private bool SatisifySubExpression(PerformanceTrace? pt,
            ConditionLookupOptimization lookupOptimization, JObject jContent, ConditionSubset conditionSubset)
        {
            var expression = new NCalc.Expression(conditionSubset.Expression);

            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = lookupOptimization.Conditions.SubsetByKey(subsetKey);

                bool subExpressionResult = SatisifySubExpression(pt, lookupOptimization, jContent, subExpression);
                expression.Parameters[subsetKey] = subExpressionResult;
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

            var ptEvaluate = pt?.BeginTrace(PerformanceTraceType.Evaluate);
            var evaluation = (bool)expression.Evaluate();
            ptEvaluate?.EndTrace();

            return evaluation;
        }

        /// <summary>
        /// Mathematically collapses all subexpressions to return a boolean match.
        /// </summary>
        /// <param name="lookupOptimization"></param>
        /// <param name="jContent"></param>
        /// <param name="conditionSubset"></param>
        /// <returns></returns>
        /// <exception cref="KbParserException"></exception>
        private bool SatisifySubExpressionByJsonContentAlias(PerformanceTrace? pt,
            ConditionLookupOptimization lookupOptimization, Dictionary<string, JObject> jContentByAlias, ConditionSubset conditionSubset)
        {
            var expression = new NCalc.Expression(conditionSubset.Expression);

            /*
            //TODO: What do we do here?
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = lookupOptimization.Conditions.SubsetByKey(subsetKey);

                bool subExpressionResult = SatisifySubExpressionByJsonContentAlias(pt, lookupOptimization, jContentByAlias, subExpression);
                expression.Parameters[subsetKey] = subExpressionResult;
            }
            */

            var expressionString = new StringBuilder();

            foreach (var condition in conditionSubset.Conditions)
            {
                expressionString.Clear();

                Utility.EnsureNotNull(condition.Left.Value);
                Utility.EnsureNotNull(condition.Right.Value);

                var jContent = jContentByAlias[condition.Left.Prefix];

                //Get the value of the condition:
                if (!jContent.TryGetValue(condition.Left.Value, StringComparison.CurrentCultureIgnoreCase, out JToken? jLeftToken))
                {
                    throw new KbParserException($"Field not found in document [{condition.Left.Value}].");
                }

                jContent = jContentByAlias[condition.Right.Prefix];

                //Get the value of the condition:
                if (!jContent.TryGetValue(condition.Right.Value, StringComparison.CurrentCultureIgnoreCase, out JToken? jRightToken))
                {
                    throw new KbParserException($"Field not found in document [{condition.Right.Value}].");
                }

                /*
                if (expressionString.Length > 0)
                {
                    expressionString.Append(ConditionTokenizer.LogicalConnectorToOperator(condition.LogicalConnector));
                }
                var singleConditionResult =  Condition.IsMatch(jLeftToken.ToString().ToLower(), condition.LogicalQualifier, jRightToken.ToString());
                expressionString.Append(singleConditionResult ? "1==1" : "1==0");
                */

                var singleConditionResult = Condition.IsMatch(jLeftToken.ToString().ToLower(), condition.LogicalQualifier, jRightToken.ToString());

                expression.Parameters[condition.ConditionKey] = singleConditionResult;
            }

            var ptEvaluate = pt?.BeginTrace(PerformanceTraceType.Evaluate);
            var evaluation = (bool)expression.Evaluate();
            ptEvaluate?.EndTrace();

            return evaluation;
        }

        public class QuerySchemaMapItem
        {
            public PersistSchema SchemaMeta { get; set; }
            public PersistDocumentCatalog DocuemntCatalog { get; set; }
            public Conditions? Conditions { get; set; }

            public QuerySchemaMapItem(PersistSchema schemaMeta, PersistDocumentCatalog docuemntCatalog, Conditions? conditions)
            {
                SchemaMeta = schemaMeta;
                DocuemntCatalog = docuemntCatalog;
                Conditions = conditions;
            }
        }

        public class QuerySchemaMap : Dictionary<String, QuerySchemaMapItem>
        {
            public void Add(string key, PersistSchema schemaMeta, PersistDocumentCatalog docuemntCatalog, Conditions? conditions)
            {
                this.Add(key, new QuerySchemaMapItem(schemaMeta, docuemntCatalog, conditions));
            }
        }

        /// <summary>
        /// Finds all document using a prepared query.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="KbInvalidSchemaException"></exception>
        private KbQueryResult FindDocumentsByPreparedQuery(PerformanceTrace? pt, Transaction transaction, PreparedQuery query)
        {
            var result = new KbQueryResult();

            var schemaMap = new QuerySchemaMap();

            foreach (var querySchema in query.Schemas)
            {
                //Lock the schema:
                var ptLockSchema = pt?.BeginTrace<PersistSchema>(PerformanceTraceType.Lock);
                var schemaMeta = core.Schemas.VirtualPathToMeta(transaction, querySchema.Name, LockOperation.Read);
                if (schemaMeta == null || schemaMeta.Exists == false)
                {
                    throw new KbInvalidSchemaException(querySchema.Name);
                }
                ptLockSchema?.EndTrace();
                Utility.EnsureNotNull(schemaMeta.DiskPath);

                //Lock the document catalog:
                var documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, DocumentCatalogFile);
                var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(pt, transaction, documentCatalogDiskPath, LockOperation.Read);
                Utility.EnsureNotNull(documentCatalog);

                schemaMap.Add(querySchema.Alias, schemaMeta, documentCatalog, querySchema.Conditions);
            }

            if (query.SelectFields.Count == 1 && query.SelectFields[0].Key == "*")
            {
                query.SelectFields.Clear();
                throw new KbNotImplementedException("Select * is not implemented. This will require schema scampling.");
            }
            else if (query.SelectFields.Count == 0)
            {
                query.SelectFields.Clear();
                throw new KbNotImplementedException("No fields were selected.");
            }

            //Figure out which indexes could assist us in retrieving the desired documents (if any).
            var ptOptimization = pt?.BeginTrace(PerformanceTraceType.Optimization);
            var lookupOptimization = core.Indexes.SelectIndexesForConditionLookupOptimization(transaction, schemaMap.First().Value.SchemaMeta, query.Conditions);
            ptOptimization?.EndTrace();

            /*
             *  We need to build a generic key/value dataset which is the combined fieldset from each inner joined document.
             *  Then we use the conditions that were supplied to eliminate results from that dataset.
            */

            if (schemaMap.Count > 1)
            {
                var schemaMapResults = JoinSchemaMaps(pt, transaction, schemaMap, query, lookupOptimization);
            }
            else
            {

            }

            //var subsetResults = GetAllDocumentsByConditions(pt, transaction, schemaMap, query, lookupOptimization);

            /*
            foreach (var field in query.SelectFields)
            {
                result.Fields.Add(new KbQueryField(field.Alias));
            }

            foreach (var subsetResult in subsetResults.Collection)
            {
                result.Rows.Add(new KbQueryRow(subsetResult.Values));
            }
            */

            return result;
        }


        /// <summary>
        /// Build a generic key/value dataset which is the combined fieldset from each inner joined document.
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="transaction"></param>
        /// <param name="schemaMap"></param>
        /// <param name="query"></param>
        /// <param name="lookupOptimization"></param>
        /// <returns></returns>
        private SchemaMapResults JoinSchemaMaps(PerformanceTrace? pt, Transaction transaction,
            QuerySchemaMap schemaMap, PreparedQuery query, ConditionLookupOptimization lookupOptimization)
        {
            var results = new SchemaMapResults();
            //Here we should evaluate the join conditions (and probably the supplied actual conditions)
            //  to see if we can do some early document elimination. We should also evaluate the indexes
            //  for use on the join clause.

            var topLevel = schemaMap.First();
            var topLevelMap = topLevel.Value;

            Utility.EnsureNotNull(topLevelMap.SchemaMeta);
            Utility.EnsureNotNull(topLevelMap.SchemaMeta.DiskPath);

            foreach (var toplevelDocument in topLevelMap.DocuemntCatalog.Collection)
            {
                var persistDocumentDiskPathTopLevel = Path.Combine(topLevelMap.SchemaMeta.DiskPath, toplevelDocument.FileName);

                var persistDocumentTopLevel = core.IO.GetJson<PersistDocument>(pt, transaction, persistDocumentDiskPathTopLevel, LockOperation.Read);
                Utility.EnsureNotNull(persistDocumentTopLevel);
                Utility.EnsureNotNull(persistDocumentTopLevel.Content);

                var jContentTopLevel = JObject.Parse(persistDocumentTopLevel.Content);

                var jContentByAlias = new Dictionary<string, JObject>
                {
                    { topLevel.Key, jContentTopLevel } //Start with the docuemnt from the top level.
                };

                foreach (var nextLevel in schemaMap.Skip(1))
                {
                    var nextLevelMap = nextLevel.Value;

                    Utility.EnsureNotNull(nextLevelMap.SchemaMeta);
                    Utility.EnsureNotNull(nextLevelMap.SchemaMeta.DiskPath);

                    foreach (var nextLevelDocument in nextLevelMap.DocuemntCatalog.Collection)
                    {
                        var persistDocumentDiskPathNextLevel = Path.Combine(nextLevelMap.SchemaMeta.DiskPath, nextLevelDocument.FileName);

                        var persistDocumentNextLevel = core.IO.GetJson<PersistDocument>(pt, transaction, persistDocumentDiskPathNextLevel, LockOperation.Read);
                        Utility.EnsureNotNull(persistDocumentNextLevel);
                        Utility.EnsureNotNull(persistDocumentNextLevel.Content);

                        var jContentNextLevel = JObject.Parse(persistDocumentNextLevel.Content);

                        jContentByAlias.Add(nextLevel.Key, jContentNextLevel);

                        Utility.EnsureNotNull(nextLevelMap.Conditions);

                        var expression = new NCalc.Expression(nextLevelMap.Conditions.Root.Expression);

                        //If we have subsets, then we need to satisify those in order to complete the equation.
                        foreach (var subsetKey in nextLevelMap.Conditions.Root.SubsetKeys)
                        {
                            var subExpression = nextLevelMap.Conditions.SubsetByKey(subsetKey);

                            bool subExpressionResult = SatisifySubExpressionByJsonContentAlias(pt, lookupOptimization, jContentByAlias, subExpression);
                            expression.Parameters[subsetKey] = subExpressionResult;
                        }

                        var ptEvaluate = pt?.BeginTrace(PerformanceTraceType.Evaluate);
                        bool evaluation = (bool)expression.Evaluate();
                        ptEvaluate?.EndTrace();

                        if (evaluation)
                        {
                            if (results.SchemaRIDs.ContainsKey(topLevel.Key) == false)
                            {
                                results.SchemaRIDs.Add(topLevel.Key, new HashSet<Guid>());
                            }
                            results.SchemaRIDs[topLevel.Key].Add(toplevelDocument.Id);

                            if (results.SchemaRIDs.ContainsKey(nextLevel.Key) == false)
                            {
                                results.SchemaRIDs.Add(nextLevel.Key, new HashSet<Guid>());
                            }
                            results.SchemaRIDs[nextLevel.Key].Add(nextLevelDocument.Id);

                            results.Add(new SchemaMapResult()); //TODO: add values.
                        }

                        jContentByAlias.Remove(nextLevel.Key);//We are no longer working with the document at this level.
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Saves a new document, this is used for inserts.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        /// <param name="newId"></param>
        /// <exception cref="KbInvalidSchemaException"></exception>
        public void Store(ulong processId, string schema, KbDocument document, out Guid? newId)
        {
            try
            {
                var persistDocument = PersistDocument.FromPayload(document);

                if (persistDocument.Id == null || persistDocument.Id == Guid.Empty)
                {
                    persistDocument.Id = Guid.NewGuid();
                }
                if (persistDocument.Created == null || persistDocument.Created == DateTime.MinValue)
                {
                    persistDocument.Created = DateTime.UtcNow;
                }
                if (persistDocument.Modfied == null || persistDocument.Modfied == DateTime.MinValue)
                {
                    persistDocument.Modfied = DateTime.UtcNow;
                }

                using (var txRef = core.Transactions.Begin(processId))
                {
                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Write);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(schema);
                    }
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    string documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, DocumentCatalogFile);

                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(txRef.Transaction, documentCatalogDiskPath, LockOperation.Write);
                    Utility.EnsureNotNull(documentCatalog);

                    documentCatalog.Add(persistDocument);
                    core.IO.PutJson(txRef.Transaction, documentCatalogDiskPath, documentCatalog);

                    string documentDiskPath = Path.Combine(schemaMeta.DiskPath, Helpers.GetDocumentModFilePath((Guid)persistDocument.Id));
                    core.IO.CreateDirectory(txRef.Transaction, Path.GetDirectoryName(documentDiskPath));
                    core.IO.PutJson(txRef.Transaction, documentDiskPath, persistDocument);

                    core.Indexes.InsertDocumentIntoIndexes(txRef.Transaction, schemaMeta, persistDocument);

                    newId = persistDocument.Id;

                    txRef.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to store document for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes a document by its ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <param name="newId"></param>
        /// <exception cref="KbInvalidSchemaException"></exception>
        public void DeleteById(ulong processId, string schema, Guid newId)
        {
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Write);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(schema);
                    }

                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    string documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, DocumentCatalogFile);

                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(txRef.Transaction, documentCatalogDiskPath, LockOperation.Write);

                    Utility.EnsureNotNull(documentCatalog);

                    var persistDocument = documentCatalog.GetById(newId);
                    if (persistDocument != null)
                    {
                        string documentDiskPath = Path.Combine(schemaMeta.DiskPath, Helpers.GetDocumentModFilePath(persistDocument.Id));

                        core.IO.DeleteFile(txRef.Transaction, documentDiskPath);

                        documentCatalog.Remove(persistDocument);

                        core.Indexes.DeleteDocumentFromIndexes(txRef.Transaction, schemaMeta, persistDocument.Id);

                        core.IO.PutJson(txRef.Transaction, documentCatalogDiskPath, documentCatalog);
                    }

                    txRef.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete document by ID for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns a list of all documents in a schema.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        /// <exception cref="KbInvalidSchemaException"></exception>
        public List<PersistDocumentCatalogItem> EnumerateCatalog(ulong processId, string schema)
        {
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    PersistSchema schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(schema);
                    }
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    var filePath = Path.Combine(schemaMeta.DiskPath, DocumentCatalogFile);
                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(txRef.Transaction, filePath, LockOperation.Read);
                    Utility.EnsureNotNull(documentCatalog);

                    var list = new List<PersistDocumentCatalogItem>();
                    foreach (var item in documentCatalog.Collection)
                    {
                        list.Add(item);
                    }
                    txRef.Commit();

                    return list;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to get catalog for process {processId}.", ex);
                throw;
            }
        }
    }
}
