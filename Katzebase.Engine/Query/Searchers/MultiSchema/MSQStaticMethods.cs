using Katzebase.Engine.Documents;
using Katzebase.Engine.Indexes;
using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Query.Searchers.MultiSchema.Intersection;
using Katzebase.Engine.Query.Searchers.MultiSchema.Mapping;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PrivateLibrary;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Newtonsoft.Json.Linq;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Query.Searchers.MultiSchema
{
    internal static class MSQStaticMethods
    {
        /// <summary>
        /// Build a generic key/value dataset which is the combined fieldset from each inner joined document.
        /// </summary>
        internal static MSQDocumentLookupResults GetDocumentsByConditions(Core core, PerformanceTrace? pt, Transaction transaction,
            MSQQuerySchemaMap schemaMap, PreparedQuery query)
        {
            //Here we should evaluate the join conditions (and probably the supplied actual conditions).

            var topLevel = schemaMap.First();
            var topLevelMap = topLevel.Value;

            Utility.EnsureNotNull(topLevelMap.SchemaMeta);
            Utility.EnsureNotNull(topLevelMap.SchemaMeta.DiskPath);

            //TODO: We should put some intelligence behind the thread count.
            var ptThreadCreation = pt?.BeginTrace(PerformanceTraceType.ThreadCreation);
            var threadParam = new LookupThreadParam(core, pt, transaction, schemaMap, query);
            int threadCount = ThreadPoolHelper.CalculateThreadCount(topLevelMap.DocuemntCatalog.Collection.Count);
            //int threadCount = 1;
            var threadPool = ThreadPoolQueue<PersistDocumentCatalogItem, LookupThreadParam>.CreateAndStart(LookupThreadProc, threadParam, threadCount);
            ptThreadCreation?.EndTrace();

            foreach (var toplevelDocument in topLevelMap.DocuemntCatalog.Collection)
            {
                if (threadPool.HasException || threadPool.ContinueToProcessQueue == false)
                {
                    break;
                }

                threadPool.EnqueueWorkItem(toplevelDocument);
            }

            var ptThreadCompletion = pt?.BeginTrace(PerformanceTraceType.ThreadCompletion);
            threadPool.WaitForCompletion();
            ptThreadCompletion?.EndTrace();

            return threadParam.Results;
        }

        private class LookupThreadParam
        {
            public MSQDocumentLookupResults Results = new();
            public MSQQuerySchemaMap SchemaMap { get; private set; }
            public Core Core { get; private set; }
            public PerformanceTrace? PT { get; private set; }
            public Transaction Transaction { get; private set; }
            public PreparedQuery Query { get; private set; }

            public LookupThreadParam(Core core, PerformanceTrace? pt, Transaction transaction,
                MSQQuerySchemaMap schemaMap, PreparedQuery query)
            {
                Core = core;
                PT = pt;
                Transaction = transaction;
                SchemaMap = schemaMap;
                Query = query;
            }
        }

        private static void LookupThreadProc(ThreadPoolQueue<PersistDocumentCatalogItem, LookupThreadParam> pool, LookupThreadParam? param)
        {
            Utility.EnsureNotNull(param);

            while (pool.ContinueToProcessQueue)
            {
                var toplevelDocument = pool.DequeueWorkItem();
                if (toplevelDocument == null)
                {
                    continue;
                }

                FindDocumentsOfSchemas(param, toplevelDocument);
            }
        }

        private static void FindDocumentsOfSchemas(LookupThreadParam param, PersistDocumentCatalogItem workingDocument)
        {
            var cumulativeResults = new MSQSchemaIntersectionDocumentCollection();

            var jContentByAlias = new Dictionary<string, JObject>();
            var topLevel = param.SchemaMap.First();

            Utility.EnsureNotNull(topLevel.Value.SchemaMeta.DiskPath);

            var persistDocumentDiskPathWorkingLevel = Path.Combine(topLevel.Value.SchemaMeta.DiskPath, workingDocument.FileName);

            var persistDocumentWorkingLevel = param.Core.IO.GetJson<PersistDocument>(param.PT, param.Transaction, persistDocumentDiskPathWorkingLevel, LockOperation.Read);
            Utility.EnsureNotNull(persistDocumentWorkingLevel);
            Utility.EnsureNotNull(persistDocumentWorkingLevel.Content);

            var jWorkingContent = JObject.Parse(persistDocumentWorkingLevel.Content);
            jContentByAlias.Add(topLevel.Key, jWorkingContent);

            lock (cumulativeResults)
            {
                if (cumulativeResults.MatchedDocumentIDsPerSchema.TryGetValue(topLevel.Key, out HashSet<Guid>? documentIDs))
                {
                    documentIDs.Add(workingDocument.Id);
                }
                else
                {
                    documentIDs = new HashSet<Guid> { workingDocument.Id };
                    cumulativeResults.MatchedDocumentIDsPerSchema.Add(topLevel.Key, documentIDs);
                }
            }

            FindDocumentsOfSchemasRecursive(param, workingDocument, topLevel, 1, ref cumulativeResults, jContentByAlias);

            //Take all of the found schama/document IDs and acculumate the doucment values here.
            if (cumulativeResults.MatchedDocumentIDsPerSchema.Count == param.SchemaMap.Count)
            {
                var schemaResults = cumulativeResults.MatchedDocumentIDsPerSchema.OrderByDescending(o => o.Value.Count);
                var schemaResult = schemaResults.First();

                var topLevelAccumulationMap = param.SchemaMap[schemaResult.Key];
                var topLevelDocumentIDs = cumulativeResults.MatchedDocumentIDsPerSchema[schemaResult.Key];

                var resultRows = new MSQDocumentLookupResults();

                foreach (var documentID in topLevelDocumentIDs)
                {
                    var rowValues = new MSQDocumentLookupResult(documentID);

                    FillInSchemaResultDocumentValues(param, topLevelAccumulationMap, schemaResult.Key, documentID, ref rowValues);

                    foreach (var nextResult in schemaResults.Skip(1))
                    {
                        var nextLevelAccumulationMap = param.SchemaMap[nextResult.Key];
                        var nextLevelDocumentIDs = cumulativeResults.MatchedDocumentIDsPerSchema[nextResult.Key];

                        foreach (var nextLevelDocumentID in nextLevelDocumentIDs)
                        {
                            FillInSchemaResultDocumentValues(param, nextLevelAccumulationMap, nextResult.Key, nextLevelDocumentID, ref rowValues);
                        }
                    }

                    resultRows.Add(rowValues);
                }

                lock (param.Results)
                {
                    param.Results.AddRange(resultRows);
                }
            }
        }

        private static void FillInSchemaResultDocumentValues(LookupThreadParam param, MSQQuerySchemaMapItem accumulationMap,
            string schemaKey, Guid documentID, ref MSQDocumentLookupResult rowValues)
        {
            Utility.EnsureNotNull(accumulationMap?.SchemaMeta?.DiskPath);
            var documentFileName = Helpers.GetDocumentModFilePath(documentID);
            var persistDocumentDiskPath = Path.Combine(accumulationMap.SchemaMeta.DiskPath, documentFileName);

            var persistDocument = param.Core.IO.GetJson<PersistDocument>(param.PT, param.Transaction, persistDocumentDiskPath, LockOperation.Read);
            Utility.EnsureNotNull(persistDocument);
            Utility.EnsureNotNull(persistDocument.Content);

            var jIndexContent = JObject.Parse(persistDocument.Content);

            foreach (var selectField in param.Query.SelectFields.Where(o => o.SchemaAlias == schemaKey))
            {
                if (!jIndexContent.TryGetValue(selectField.Key, StringComparison.CurrentCultureIgnoreCase, out JToken? token))
                {
                    throw new KbParserException($"Field not found: {schemaKey}.{selectField}.");
                }

                rowValues.Values.Add(token?.ToString() ?? "");
            }
        }


        private static void FindDocumentsOfSchemasRecursive(LookupThreadParam param, PersistDocumentCatalogItem workingDocument, KeyValuePair<string,
            MSQQuerySchemaMapItem> workingLevel, int skipCount, ref MSQSchemaIntersectionDocumentCollection cumulativeResults, Dictionary<string, JObject> jContentByAlias)
        {
            var thisThreadResults = new Dictionary<Guid, MSQSchemaIntersectionDocumentCollection>();

            var workingLevelMap = workingLevel.Value;

            var nextLevel = param.SchemaMap.Skip(skipCount).First();
            var nextLevelMap = nextLevel.Value;

            Utility.EnsureNotNull(nextLevelMap?.Conditions);
            Utility.EnsureNotNull(nextLevelMap?.SchemaMeta?.DiskPath);
            Utility.EnsureNotNull(workingLevelMap?.SchemaMeta?.DiskPath);

            var jWorkingContent = jContentByAlias[workingLevel.Key];

            var expression = new NCalc.Expression(nextLevelMap.Conditions.HighLevelExpressionTree);

            #region New indexing stuff..

            //Create a reference to the entire document catalog.
            var limitedDocumentCatalogItems = nextLevelMap.DocuemntCatalog.Collection;

            if (nextLevelMap.Optimization?.CanApplyIndexing() == true)
            {
                //We are going to create a limited document catalog from the indexes. So kill the reference and create an empty list.
                limitedDocumentCatalogItems = new List<PersistDocumentCatalogItem>();

                //All condition subsets have a selected index. Start building a list of possible document IDs.
                foreach (var subset in nextLevelMap.Optimization.Conditions.NonRootSubsets)
                {
                    Utility.EnsureNotNull(subset.IndexSelection?.Index?.DiskPath);
                    Utility.EnsureNotNull(subset.IndexSelection?.Index?.Id);

                    var indexPageCatalog = param.Core.IO.GetPBuf<PersistIndexPageCatalog>(param.PT, param.Transaction, subset.IndexSelection.Index.DiskPath, LockOperation.Read);
                    Utility.EnsureNotNull(indexPageCatalog);

                    var keyValuePairs = new Dictionary<string, string>();

                    //Grab the values from the schema above and save them for the index lookup of the next schema in the join.
                    foreach (var condition in subset.Conditions)
                    {
                        var jIndexContent = jContentByAlias[condition.Right?.Prefix ?? ""];

                        if (!jIndexContent.TryGetValue((condition.Right?.Value ?? ""), StringComparison.CurrentCultureIgnoreCase, out JToken? conditionToken))
                        {
                            throw new KbParserException($"Join clause field not found in document [{workingLevel.Key}].");
                        }
                        keyValuePairs.Add(condition.Left?.Value ?? "", conditionToken?.ToString() ?? "");
                    }

                    //Match on values from the document.
                    var documentIds = param.Core.Indexes.MatchDocuments(param.PT, indexPageCatalog, subset.IndexSelection, subset, keyValuePairs);

                    limitedDocumentCatalogItems.AddRange(nextLevelMap.DocuemntCatalog.Collection.Where(o => documentIds.Contains(o.Id)).ToList());
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

            #endregion

            int thisSchemaMatchCount = 0;

            foreach (var nextLevelDocument in limitedDocumentCatalogItems)
            {
                var persistDocumentDiskPathNextLevel = Path.Combine(nextLevelMap.SchemaMeta.DiskPath, nextLevelDocument.FileName);

                var persistDocumentNextLevel = param.Core.IO.GetJson<PersistDocument>(param.PT, param.Transaction, persistDocumentDiskPathNextLevel, LockOperation.Read);
                Utility.EnsureNotNull(persistDocumentNextLevel?.Content);

                var jContentNextLevel = JObject.Parse(persistDocumentNextLevel.Content);

                jContentByAlias.Add(nextLevel.Key, jContentNextLevel);

                SetExpressionParameters(ref expression, nextLevelMap.Conditions, jContentByAlias);

                var ptEvaluate = param.PT?.BeginTrace(PerformanceTraceType.Evaluate);
                bool evaluation = (bool)expression.Evaluate();
                ptEvaluate?.EndTrace();

                if (evaluation)
                {
                    thisSchemaMatchCount++;

                    if (thisSchemaMatchCount > 1) //Clearly a 1-to-many join.
                    {
                        //And, maybe we show this in the "plan"?
                    }

                    lock (cumulativeResults)
                    {
                        if (cumulativeResults.MatchedDocumentIDsPerSchema.TryGetValue(nextLevel.Key, out HashSet<Guid>? documentIDs))
                        {
                            documentIDs.Add(nextLevelDocument.Id);
                        }
                        else
                        {
                            documentIDs = new HashSet<Guid> { nextLevelDocument.Id };
                            cumulativeResults.MatchedDocumentIDsPerSchema.Add(nextLevel.Key, documentIDs);
                        }
                    }

                    if (skipCount < param.SchemaMap.Count - 1)
                    {
                        //This is wholly untested.
                        FindDocumentsOfSchemasRecursive(param, nextLevelDocument, nextLevel, skipCount + 1, ref cumulativeResults, jContentByAlias);
                    }

                    if (thisThreadResults.TryGetValue(workingDocument.Id, out MSQSchemaIntersectionDocumentCollection? docuemntCollection) == false)
                    {
                        docuemntCollection = new MSQSchemaIntersectionDocumentCollection();
                        thisThreadResults.Add(workingDocument.Id, docuemntCollection);
                        docuemntCollection.Documents.Add(new MSQSchemaIntersectionDocumentItem(workingLevel.Key, workingDocument.Id));
                    }

                    docuemntCollection.Documents.Add(new MSQSchemaIntersectionDocumentItem(nextLevel.Key, nextLevelDocument.Id));
                }

                jContentByAlias.Remove(nextLevel.Key);//We are no longer working with the document at this level.
            }

            jContentByAlias.Remove(workingLevel.Key);//We are no longer working with the document at this level.
        }

        /// <summary>
        /// Gets the json content values for the specified conditions.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="conditions"></param>
        /// <param name="jContent"></param>
        private static void SetExpressionParameters(ref NCalc.Expression expression, Conditions conditions, Dictionary<string, JObject> jContentByAlias)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditions.Root.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetExpressionParametersRecursive(ref expression, conditions, subExpression, jContentByAlias);
            }
        }

        private static void SetExpressionParametersRecursive(ref NCalc.Expression expression, Conditions conditions, ConditionSubset conditionSubset, Dictionary<string, JObject> jContentByAlias)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetExpressionParametersRecursive(ref expression, conditions, subExpression, jContentByAlias);
            }

            foreach (var condition in conditionSubset.Conditions)
            {
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

                var singleConditionResult = Condition.IsMatch(jLeftToken.ToString().ToLower(), condition.LogicalQualifier, jRightToken.ToString());

                expression.Parameters[condition.ConditionKey] = singleConditionResult;
            }
        }

    }
}
