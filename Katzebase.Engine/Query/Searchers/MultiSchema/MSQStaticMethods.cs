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
            var jThreadScopedContentCache = new Dictionary<string, JObject>();

            var cumulativeResults = new MSQSchemaIntersectionDocumentCollection();

            var jJoinScopedContentCache = new Dictionary<string, JObject>();
            var topLevel = param.SchemaMap.First();

            Utility.EnsureNotNull(topLevel.Value.SchemaMeta.DiskPath);

            var persistDocumentDiskPathWorkingLevel = Path.Combine(topLevel.Value.SchemaMeta.DiskPath, workingDocument.FileName);

            var persistDocumentWorkingLevel = param.Core.IO.GetJson<PersistDocument>(param.PT, param.Transaction, persistDocumentDiskPathWorkingLevel, LockOperation.Read);
            Utility.EnsureNotNull(persistDocumentWorkingLevel);
            Utility.EnsureNotNull(persistDocumentWorkingLevel.Content);

            //Get the document content and add it to a collection so it can be referenced by schema alias on all subsequent joins.

            var jToBeCachedContent = JObject.Parse(persistDocumentWorkingLevel.Content);
            jJoinScopedContentCache.Add(topLevel.Key, jToBeCachedContent);
            jThreadScopedContentCache.Add($"{topLevel.Key}:{persistDocumentWorkingLevel.Id}", jToBeCachedContent);

            if (cumulativeResults.MatchedDocumentIDsPerSchema.TryGetValue(topLevel.Key, out HashSet<Guid>? documentIDs))
            {
                documentIDs.Add(workingDocument.Id);
            }
            else
            {
                cumulativeResults.MatchedDocumentIDsPerSchema.Add(topLevel.Key, new HashSet<Guid> { workingDocument.Id });
            }

            FindDocumentsOfSchemasRecursive(param, workingDocument, topLevel, 1, ref cumulativeResults, jJoinScopedContentCache, jThreadScopedContentCache);

            //Take all of the found schama/document IDs and acculumate the doucment values here.
            if (cumulativeResults.MatchedDocumentIDsPerSchema.Count == param.SchemaMap.Count)
            {
                //This is an inner join that may include one-to-many, many-to-one, one-to-one or many-to-many joins.
                //Lets grab the schema results with the most rows and consider that the number of resutls we are expecting.
                //TODO: Think this though, does this work for one-to-many, many-to-one, one-to-one AND many-to-many joins.
                var allSchemasResults = cumulativeResults.MatchedDocumentIDsPerSchema.OrderByDescending(o => o.Value.Count);

                var firstSchemaResult = allSchemasResults.First();
                var firstSchemaMap = param.SchemaMap[firstSchemaResult.Key];

                var schemaResultRows = new MSQDocumentLookupResults();

                foreach (var documentID in cumulativeResults.MatchedDocumentIDsPerSchema[firstSchemaResult.Key])
                {
                    var schemaResultValues = new MSQDocumentLookupResult(documentID, param.Query.SelectFields.Count);

                    //Add the values from the top level schema.
                    FillInSchemaResultDocumentValues(param, firstSchemaMap, firstSchemaResult.Key, documentID, ref schemaResultValues, jThreadScopedContentCache);

                    //Fill in the values from all of the other schemas.
                    foreach (var nextResult in allSchemasResults.Skip(1))
                    {
                        var nextLevelAccumulationMap = param.SchemaMap[nextResult.Key];
                        var nextLevelDocumentIDs = cumulativeResults.MatchedDocumentIDsPerSchema[nextResult.Key];

                        foreach (var nextLevelDocumentID in nextLevelDocumentIDs)
                        {
                            FillInSchemaResultDocumentValues(param, nextLevelAccumulationMap, nextResult.Key, nextLevelDocumentID, ref schemaResultValues, jThreadScopedContentCache);
                        }
                    }

                    schemaResultRows.Add(schemaResultValues);
                }

                lock (param.Results)
                {
                    param.Results.AddRange(schemaResultRows);
                }
            }
        }

        private static void FillInSchemaResultDocumentValues(LookupThreadParam param, MSQQuerySchemaMapItem accumulationMap,
            string schemaKey, Guid documentID, ref MSQDocumentLookupResult schemaResultValues, Dictionary<string, JObject> jThreadScopedContentCache)
        {
            Utility.EnsureNotNull(accumulationMap?.SchemaMeta?.DiskPath);
            var documentFileName = Helpers.GetDocumentModFilePath(documentID);
            var persistDocumentDiskPath = Path.Combine(accumulationMap.SchemaMeta.DiskPath, documentFileName);

            var persistDocument = param.Core.IO.GetJson<PersistDocument>(param.PT, param.Transaction, persistDocumentDiskPath, LockOperation.Read);
            Utility.EnsureNotNull(persistDocument);
            Utility.EnsureNotNull(persistDocument.Content);

            var jIndexContent = jThreadScopedContentCache[$"{schemaKey}:{documentID}"];

            foreach (var selectField in param.Query.SelectFields.Where(o => o.SchemaAlias == schemaKey))
            {
                if (!jIndexContent.TryGetValue(selectField.Key, StringComparison.CurrentCultureIgnoreCase, out JToken? token))
                {
                    throw new KbParserException($"Field not found: {schemaKey}.{selectField}.");
                }

                schemaResultValues.InsertValue(selectField.Ordinal, token?.ToString() ?? "");
                //rowValues.Values.Insert(selectField.Ordinal, token?.ToString() ?? "");
            }
        }

        private static void FindDocumentsOfSchemasRecursive(LookupThreadParam param, PersistDocumentCatalogItem workingDocument, KeyValuePair<string,
            MSQQuerySchemaMapItem> workingLevel, int skipCount, ref MSQSchemaIntersectionDocumentCollection cumulativeResults,
            Dictionary<string, JObject> jJoinScopedContentCache, Dictionary<string, JObject> jThreadScopedContentCache)
        {
            var thisThreadResults = new Dictionary<Guid, MSQSchemaIntersectionDocumentCollection>();

            var workingLevelMap = workingLevel.Value;

            var nextLevel = param.SchemaMap.Skip(skipCount).First();
            var nextLevelMap = nextLevel.Value;

            Utility.EnsureNotNull(nextLevelMap?.Conditions);
            Utility.EnsureNotNull(nextLevelMap?.SchemaMeta?.DiskPath);
            Utility.EnsureNotNull(workingLevelMap?.SchemaMeta?.DiskPath);

            var jWorkingContent = jJoinScopedContentCache[workingLevel.Key];

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
                        var jIndexContent = jJoinScopedContentCache[condition.Right?.Prefix ?? ""];

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
                string threadScopedDocuemntCacheKey = $"{nextLevel.Key}:{nextLevelDocument.Id}";

                JObject? jContentNextLevel = null;

                if (jThreadScopedContentCache.ContainsKey(threadScopedDocuemntCacheKey))
                {
                    jContentNextLevel = jThreadScopedContentCache[threadScopedDocuemntCacheKey];
                }
                else
                {
                    var persistDocumentDiskPathNextLevel = Path.Combine(nextLevelMap.SchemaMeta.DiskPath, nextLevelDocument.FileName);
                    var persistDocumentNextLevel = param.Core.IO.GetJson<PersistDocument>(param.PT, param.Transaction, persistDocumentDiskPathNextLevel, LockOperation.Read);
                    Utility.EnsureNotNull(persistDocumentNextLevel?.Content);

                    jContentNextLevel = JObject.Parse(persistDocumentNextLevel.Content);
                    jThreadScopedContentCache.Add(threadScopedDocuemntCacheKey, jContentNextLevel);
                }

                jJoinScopedContentCache.Add(nextLevel.Key, jContentNextLevel);

                SetExpressionParameters(ref expression, nextLevelMap.Conditions, jJoinScopedContentCache);

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
                        FindDocumentsOfSchemasRecursive(param, nextLevelDocument, nextLevel, skipCount + 1, ref cumulativeResults, jJoinScopedContentCache, jThreadScopedContentCache);
                    }

                    if (thisThreadResults.TryGetValue(workingDocument.Id, out MSQSchemaIntersectionDocumentCollection? docuemntCollection) == false)
                    {
                        docuemntCollection = new MSQSchemaIntersectionDocumentCollection();
                        thisThreadResults.Add(workingDocument.Id, docuemntCollection);
                        docuemntCollection.Documents.Add(new MSQSchemaIntersectionDocumentItem(workingLevel.Key, workingDocument.Id));
                    }

                    docuemntCollection.Documents.Add(new MSQSchemaIntersectionDocumentItem(nextLevel.Key, nextLevelDocument.Id));
                }

                jJoinScopedContentCache.Remove(nextLevel.Key);//We are no longer working with the document at this level.
            }

            jJoinScopedContentCache.Remove(workingLevel.Key);//We are no longer working with the document at this level.
        }

        /// <summary>
        /// Gets the json content values for the specified conditions.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="conditions"></param>
        /// <param name="jContent"></param>
        private static void SetExpressionParameters(ref NCalc.Expression expression, Conditions conditions, Dictionary<string, JObject> jJoinScopedContentCache)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditions.Root.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetExpressionParametersRecursive(ref expression, conditions, subExpression, jJoinScopedContentCache);
            }
        }

        private static void SetExpressionParametersRecursive(ref NCalc.Expression expression, Conditions conditions, ConditionSubset conditionSubset, Dictionary<string, JObject> jJoinScopedContentCache)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetExpressionParametersRecursive(ref expression, conditions, subExpression, jJoinScopedContentCache);
            }

            foreach (var condition in conditionSubset.Conditions)
            {
                Utility.EnsureNotNull(condition.Left.Value);
                Utility.EnsureNotNull(condition.Right.Value);

                var jContent = jJoinScopedContentCache[condition.Left.Prefix];

                //Get the value of the condition:
                if (!jContent.TryGetValue(condition.Left.Value, StringComparison.CurrentCultureIgnoreCase, out JToken? jLeftToken))
                {
                    throw new KbParserException($"Field not found in document [{condition.Left.Value}].");
                }

                jContent = jJoinScopedContentCache[condition.Right.Prefix];

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
