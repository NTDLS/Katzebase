using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Documents;
using Katzebase.Engine.Indexes;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Query.Function.Aggregate;
using Katzebase.Engine.Query.Function.Scaler;
using Katzebase.Engine.Query.FunctionParameter;
using Katzebase.Engine.Query.Searchers.Intersection;
using Katzebase.Engine.Query.Searchers.Mapping;
using Katzebase.Engine.Query.Sorting;
using Katzebase.Engine.Threading;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using System.Collections;
using System;
using System.Collections.Generic;
using static Katzebase.Engine.Documents.DocumentPointer;
using static Katzebase.Engine.Library.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;
using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.Engine.Query.Searchers
{
    internal static class StaticSchemaIntersectionMethods
    {
        /// <summary>
        /// Build a generic key/value dataset which is the combined fieldset from each inner joined document.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="transaction"></param>
        /// <param name="schemaMap"></param>
        /// <param name="query"></param>
        /// <param name="gatherDocumentPointersForSchemaPrefix">When not null, the process will focus on obtaining a list of DocumentPointers instead of key/values. This is used for UPDATES and DELETES.</param>
        /// <returns></returns>
        internal static DocumentLookupResults GetDocumentsByConditions(Core core, Transaction transaction,
            QuerySchemaMap schemaMap, PreparedQuery query, string? gatherDocumentPointersForSchemaPrefix = null)
        {
            var topLevel = schemaMap.First();
            var topLevelMap = topLevel.Value;

            var documentPointers = topLevelMap.DocumentPageCatalog.ConsolidatedDocumentPointers();

            ConditionLookupOptimization? lookupOptimization = null;

            //TODO: Here we should evaluate whatever conditions we can to early eliminate the top level document scans.
            //If we dont have any conditions then we just need to return all rows from the schema.
            if (query.Conditions.Subsets.Count > 0)
            {
                lookupOptimization = ConditionLookupOptimization.Build(core, transaction, topLevelMap.PhysicalSchema, query.Conditions, topLevelMap.Prefix);

                var limitedDocumentPointers = new List<DocumentPointer>();

                if (lookupOptimization.CanApplyIndexing())
                {
                    //We are going to create a limited document catalog from the indexes. So kill the reference and create an empty list.
                    documentPointers = new List<DocumentPointer>();

                    //All condition subsets have a selected index. Start building a list of possible document IDs.
                    foreach (var subset in lookupOptimization.Conditions.NonRootSubsets)
                    {
                        KbUtility.EnsureNotNull(subset.IndexSelection);

                        var physicalIndexPages = core.IO.GetPBuf<PhysicalIndexPages>(transaction, subset.IndexSelection.Index.DiskPath, LockOperation.Read);
                        var indexMatchedDocuments = core.Indexes.MatchDocuments(transaction, physicalIndexPages, subset.IndexSelection, subset, topLevelMap.Prefix);

                        limitedDocumentPointers.AddRange(indexMatchedDocuments.Select(o => o.Value));
                    }

                    documentPointers = limitedDocumentPointers;
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
                    #endregion
                }
            }

            var ptThreadCreation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCreation);
            var threadParam = new LookupThreadParam(core, transaction, schemaMap, query, gatherDocumentPointersForSchemaPrefix);
            int threadCount = ThreadPoolHelper.CalculateThreadCount(core.Sessions.ByProcessId(transaction.ProcessId), schemaMap.TotalDocumentCount());

            //threadCount = 1;

            transaction.PT?.AddDescreteMetric(PerformanceTraceDescreteMetricType.ThreadCount, threadCount);
            var threadPool = ThreadPoolQueue<DocumentPointer, LookupThreadParam>
                .CreateAndStart($"GetDocumentsByConditions:{transaction.ProcessId}", LookupThreadProc, threadParam, threadCount);
            ptThreadCreation?.StopAndAccumulate();

            foreach (var documentPointer in documentPointers)
            {
                if (threadPool.HasException || threadPool.ContinueToProcessQueue == false)
                {
                    break;
                }

                if (query.RowLimit != 0 && query.SortFields.Any() == false && threadParam.Results.Collection.Count >= query.RowLimit)
                {
                    break;
                }

                threadPool.EnqueueWorkItem(documentPointer);
            }

            var ptThreadCompletion = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCompletion);
            threadPool.WaitForCompletion();
            ptThreadCompletion?.StopAndAccumulate();

            #region Grouping.

            int GetDictonaryIndex(Dictionary<string, string?> dictionary, string searchKey)
            {
                int currentIndex = 0;
                foreach (var entry in dictionary)
                {
                    if (entry.Key == searchKey)
                    {
                        return currentIndex;
                    }
                    currentIndex++;
                }
                return -1;
            }

            if (query.GroupFields.Any() && threadParam.Results.Collection.Any())
            {
                //var firstRow = threadParam.Results.Collection.First();

                //Here we are going to build a grouping_key using the concatenated select fields.
                var groupedValues = threadParam.Results.Collection.GroupBy(arr =>
                    string.Join("\t", query.GroupFields.OfType<FunctionDocumentFieldParameter>().Select(groupFieldParam => arr.MethodFields[groupFieldParam.Value.Key])));

                var groupedResults = new SchemaIntersectionRowCollection();

                foreach (var group in groupedValues)
                {
                    var values = new List<string?>();

                    var groupValues = group.Key.Split('\t');

                    for (int i = 0; i < query.SelectFields.Count; i++)
                    {
                        var field = query.SelectFields[i];

                        if (field is FunctionDocumentFieldParameter)
                        {
                            var specific = (FunctionDocumentFieldParameter)field;
                            var specificValue = group.First().MethodFields[specific.Value.Key];
                            values.Add(specificValue);
                        }
                        else if (field is FunctionWithParams)
                        {
                            var proc = (FunctionWithParams)field;
                            var value = QueryAggregateFunctionImplementation.CollapseAllFunctionParameters(proc, group);
                            values.Add(value);
                        }
                        else
                        {
                            throw new KbNotImplementedException($"The aggregate type is not implemented.");
                        }
                    }

                    var rowResults = new SchemaIntersectionRow
                    {
                        Values = values,
                    };

                    groupedResults.Add(rowResults);
                }

                /*
                    foreach (var methodField in param.Query.SelectFields.OfType<FunctionWithParams>().Where(o=>o.FunctionType == FunctionParameterTypes.FunctionType.Scaler))
                    {
                        foreach (var row in resultingRows.Rows)
                        {
                            var methodResult = QueryScalerFunctionImplementation.CollapseAllFunctionParameters(methodField, row.MethodFields);
                            row.InsertValue(methodField.Alias, methodField.Ordinal, methodResult);
                        }
                    }
                */

                threadParam.Results = groupedResults;
            }

            #endregion

            #region Sorting.

            //Get a list of all the fields we need to sory by.
            if (query.SortFields.Any() && threadParam.Results.Collection.Any())
            {
                var sortingColumns = new List<(int fieldIndex, KbSortDirection sortDirection)>();
                foreach (var sortField in query.SortFields.OfType<SortField>())
                {
                    var field = query.SelectFields.Where(o => o.Alias == sortField.Key).FirstOrDefault();
                    KbUtility.EnsureNotNull(field);
                    sortingColumns.Add(new(field.Ordinal, sortField.SortDirection));
                }

                //Sort the results:
                var ptSorting = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Sorting);
                threadParam.Results.Collection = threadParam.Results.Collection.OrderBy(row => row.Values, new ResultValueComparer(sortingColumns)).ToList();

                ptSorting?.StopAndAccumulate();
            }

            #endregion

            //Enforce row limits.
            if (query.RowLimit > 0)
            {
                threadParam.Results.Collection = threadParam.Results.Collection.Take(query.RowLimit).ToList();
            }

            if (gatherDocumentPointersForSchemaPrefix != null)
            {
                //Distill the document pointers to a distinct list. Can we do this in the threads? Maybe prevent the dups in the first place?
                threadParam.DocumentPointers = threadParam.DocumentPointers.Distinct(new DocumentPageEqualityComparer()).ToList();
            }

            if (query.DynamicallyBuildSelectList && threadParam.Results.Collection.Count > 0)
            {
                //If this was a "select *", we may have discovered different "fields" in different documents. We need to make sure that all rows
                //  have the same number of values.

                int maxFieldCount = query.SelectFields.Count;
                foreach (var row in threadParam.Results.Collection)
                {
                    if (row.Values.Count < maxFieldCount)
                    {
                        int difference = maxFieldCount - row.Values.Count;
                        row.Values.AddRange(new string[difference]);
                    }
                }
            }

            var results = new DocumentLookupResults();

            results.AddRange(threadParam.Results);

            return results;
        }

        #region Threading.

        private class LookupThreadParam
        {
            public string? GatherDocumentPointersForSchemaPrefix { get; set; } = null;
            public SchemaIntersectionRowCollection Results { get; set; } = new();
            public List<DocumentPointer> DocumentPointers { get; set; } = new();

            public DocumentLookupResults Results_old = new();
            public QuerySchemaMap SchemaMap { get; private set; }
            public Core Core { get; private set; }
            public Transaction Transaction { get; private set; }
            public PreparedQuery Query { get; private set; }

            public LookupThreadParam(Core core, Transaction transaction, QuerySchemaMap schemaMap, PreparedQuery query, string? gatherDocumentPointersForSchemaPrefix)
            {
                GatherDocumentPointersForSchemaPrefix = gatherDocumentPointersForSchemaPrefix;
                Core = core;
                Transaction = transaction;
                SchemaMap = schemaMap;
                Query = query;
            }
        }

        private static void LookupThreadProc(ThreadPoolQueue<DocumentPointer, LookupThreadParam> pool, LookupThreadParam? param)
        {
            KbUtility.EnsureNotNull(param);

            while (pool.ContinueToProcessQueue)
            {
                param.Transaction.EnsureActive();

                var toplevelDocument = pool.DequeueWorkItem();
                if (toplevelDocument == null)
                {
                    continue;
                }

                var resultingRows = new SchemaIntersectionRowCollection();

                IntersectAllSchemas(param, toplevelDocument, ref resultingRows);

                //Limit the results by the rows that have the correct number of schema matches.
                //TODO: This could probably be used to implement OUTER JOINS.
                if (param.GatherDocumentPointersForSchemaPrefix == null)
                {
                    resultingRows.Collection = resultingRows.Collection.Where(o => o.SchemaKeys.Count == param.SchemaMap.Count).ToList();
                }
                else
                {
                    resultingRows.Collection = resultingRows.Collection.Where(o => o.SchemaDocumentPointers.Count == param.SchemaMap.Count).ToList();
                }

                //Execute functions
                {
                    foreach (var methodField in param.Query.SelectFields.OfType<FunctionWithParams>().Where(o => o.FunctionType == FunctionParameterTypes.FunctionType.Scaler))
                    {
                        foreach (var row in resultingRows.Collection)
                        {
                            var methodResult = QueryScalerFunctionImplementation.CollapseAllFunctionParameters(methodField, row.MethodFields);
                            row.InsertValue(methodField.Alias, methodField.Ordinal, methodResult);
                        }
                    }

                    foreach (var methodField in param.Query.SelectFields.OfType<FunctionExpression>())
                    {
                        foreach (var row in resultingRows.Collection)
                        {
                            var methodResult = QueryScalerFunctionImplementation.CollapseAllFunctionParameters(methodField, row.MethodFields);
                            row.InsertValue(methodField.Alias, methodField.Ordinal, methodResult);
                        }
                    }
                }

                lock (param.Results)
                {
                    //Accumulate the results up to the parent.
                    if (param.GatherDocumentPointersForSchemaPrefix == null)
                    {
                        param.Results.AddRange(resultingRows);
                    }
                    else
                    {
                        param.DocumentPointers.AddRange(resultingRows.Collection.Select(o => o.SchemaDocumentPointers[param.GatherDocumentPointersForSchemaPrefix]));
                    }
                }
            }
        }

        #endregion

        private static void IntersectAllSchemas(LookupThreadParam param, DocumentPointer topLevelDocumentPointer, ref SchemaIntersectionRowCollection resultingRows)
        {
            var topLevelSchemaMap = param.SchemaMap.First();

            var toplevelPhysicalDocument = param.Core.Documents.AcquireDocument(param.Transaction, topLevelSchemaMap.Value.PhysicalSchema, topLevelDocumentPointer.DocumentId, LockOperation.Read);

            var jObjectToBeCachedContent = JObject.Parse(toplevelPhysicalDocument.Content);

            var threadScopedContentCache = new Dictionary<string, JObject>
            {
                { $"{topLevelSchemaMap.Key}:{topLevelDocumentPointer.Key}", jObjectToBeCachedContent }
            };

            //This cache is used to store the content of all documents required for a single row join.
            var joinScopedContentCache = new Dictionary<string, JObject>
            {
                { topLevelSchemaMap.Key, jObjectToBeCachedContent }
            };

            var resultingRow = new SchemaIntersectionRow();
            lock (resultingRows)
            {
                resultingRows.Add(resultingRow);
            }

            if (param.GatherDocumentPointersForSchemaPrefix != null)
            {
                resultingRow.AddSchemaDocumentPointer(topLevelSchemaMap.Key, topLevelDocumentPointer);
            }

            FillInSchemaResultDocumentValues(param, topLevelSchemaMap.Key, topLevelDocumentPointer, ref resultingRow, threadScopedContentCache);

            if (param.SchemaMap.Count > 1)
            {
                IntersectAllSchemasRecursive(param, 1, ref resultingRow, ref resultingRows, ref threadScopedContentCache, ref joinScopedContentCache);
            }

            if (param.Query.Conditions.AllFields.Any())
            {
                //Filter the rows by the global conditions.
                resultingRows.Collection = ApplyQueryGlobalConditions(param, resultingRows);
            }
        }

        private static void IntersectAllSchemasRecursive(LookupThreadParam param,
            int skipSchemaCount, ref SchemaIntersectionRow resultingRow, ref SchemaIntersectionRowCollection resultingRows,
            ref Dictionary<string, JObject> threadScopedContentCache, ref Dictionary<string, JObject> joinScopedContentCache)
        {
            var currentSchemaKVP = param.SchemaMap.Skip(skipSchemaCount).First();
            var currentSchemaMap = currentSchemaKVP.Value;

            KbUtility.EnsureNotNull(currentSchemaMap?.Conditions);

            var expression = new NCalc.Expression(currentSchemaMap.Conditions.HighLevelExpressionTree);

            //Create a reference to the entire document catalog.
            var limitedDocumentPointers = currentSchemaMap.DocumentPageCatalog.ConsolidatedDocumentPointers();

            #region Indexing to reduce the number of document pointers in "limitedDocumentPointers".

            if (currentSchemaMap.Optimization?.CanApplyIndexing() == true)
            {
                //We are going to create a limited document catalog from the indexes. So kill the reference and create an empty list.
                var furtherLimitedDocumentPointers = new List<DocumentPointer>();

                //All condition subsets have a selected index. Start building a list of possible document IDs.
                foreach (var subset in currentSchemaMap.Optimization.Conditions.NonRootSubsets)
                {
                    KbUtility.EnsureNotNull(subset.IndexSelection);

                    var physicalIndexPages = param.Core.IO.GetPBuf<PhysicalIndexPages>(param.Transaction, subset.IndexSelection.Index.DiskPath, LockOperation.Read);

                    var keyValuePairs = new Dictionary<string, string>();

                    //Grab the values from the schema above and save them for the index lookup of the next schema in the join.
                    foreach (var condition in subset.Conditions)
                    {
                        var jIndexContent = joinScopedContentCache[condition.Right?.Prefix ?? ""];

                        if (!jIndexContent.TryGetValue(condition.Right?.Value ?? "", StringComparison.CurrentCultureIgnoreCase, out JToken? conditionToken))
                        {
                            throw new KbEngineException($"Join clause field not found in document [{currentSchemaKVP.Key}].");
                        }
                        keyValuePairs.Add(condition.Left?.Value ?? "", conditionToken?.ToString() ?? "");
                    }

                    //Match on values from the document.
                    var documentIds = param.Core.Indexes.MatchDocuments(param.Transaction, physicalIndexPages, subset.IndexSelection, subset, keyValuePairs);

                    furtherLimitedDocumentPointers.AddRange(documentIds.Values);
                }

                limitedDocumentPointers = furtherLimitedDocumentPointers;
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

            int matchesFromThisSchema = 0;

            var rowTemplate = resultingRow.Clone();

            foreach (var documentPointer in limitedDocumentPointers)
            {
                string threadScopedDocuemntCacheKey = $"{currentSchemaKVP.Key}:{documentPointer.Key}";

                //Get the document content from the thread cache (or cache it).
                if (threadScopedContentCache.TryGetValue(threadScopedDocuemntCacheKey, out JObject? jContentNextLevel) == false)
                {
                    var physicalDocumentNextLevel = param.Core.Documents.AcquireDocument(param.Transaction, currentSchemaMap.PhysicalSchema, documentPointer.DocumentId, LockOperation.Read);
                    jContentNextLevel = JObject.Parse(physicalDocumentNextLevel.Content);
                    threadScopedContentCache.Add(threadScopedDocuemntCacheKey, jContentNextLevel);
                }

                joinScopedContentCache.Add(currentSchemaKVP.Key, jContentNextLevel);

                SetSchemaIntersectionExpressionParameters(ref expression, currentSchemaMap.Conditions, joinScopedContentCache);

                var ptEvaluate = param.Transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Evaluate);
                bool evaluation = (bool)expression.Evaluate();
                ptEvaluate?.StopAndAccumulate();

                if (evaluation)
                {
                    matchesFromThisSchema++;

                    if (matchesFromThisSchema > 1)
                    {
                        resultingRow = rowTemplate.Clone();
                        lock (resultingRows)
                        {
                            resultingRows.Add(resultingRow);
                        }
                    }

                    if (param.GatherDocumentPointersForSchemaPrefix != null)
                    {
                        resultingRow.AddSchemaDocumentPointer(currentSchemaKVP.Key, documentPointer);
                    }

                    FillInSchemaResultDocumentValues(param, currentSchemaKVP.Key, documentPointer, ref resultingRow, threadScopedContentCache);

                    if (skipSchemaCount < param.SchemaMap.Count - 1)
                    {
                        IntersectAllSchemasRecursive(param, skipSchemaCount + 1, ref resultingRow, ref resultingRows, ref threadScopedContentCache, ref joinScopedContentCache);
                    }
                }

                joinScopedContentCache.Remove(currentSchemaKVP.Key);//We are no longer working with the document at this level.
            }
        }

        #region Schema inersection.

        /// <summary>
        /// Gets the json content values for the specified conditions.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="conditions"></param>
        /// <param name="jContent"></param>
        private static void SetSchemaIntersectionExpressionParameters(ref NCalc.Expression expression, Conditions conditions, Dictionary<string, JObject> jJoinScopedContentCache)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditions.Root.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetSchemaIntersectionExpressionParametersRecursive(ref expression, conditions, subExpression, jJoinScopedContentCache);
            }
        }

        private static void SetSchemaIntersectionExpressionParametersRecursive(ref NCalc.Expression expression, Conditions conditions, ConditionSubset conditionSubset, Dictionary<string, JObject> jJoinScopedContentCache)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetSchemaIntersectionExpressionParametersRecursive(ref expression, conditions, subExpression, jJoinScopedContentCache);
            }

            foreach (var condition in conditionSubset.Conditions)
            {
                KbUtility.EnsureNotNull(condition.Left.Value);
                KbUtility.EnsureNotNull(condition.Right.Value);

                var jContent = jJoinScopedContentCache[condition.Left.Prefix];

                //Get the value of the condition:
                if (!jContent.TryGetValue(condition.Left.Value, StringComparison.CurrentCultureIgnoreCase, out JToken? jLeftToken))
                {
                    throw new KbEngineException($"Field not found in document [{condition.Left.Value}].");
                }

                jContent = jJoinScopedContentCache[condition.Right.Prefix];

                //Get the value of the condition:
                if (!jContent.TryGetValue(condition.Right.Value, StringComparison.CurrentCultureIgnoreCase, out JToken? jRightToken))
                {
                    throw new KbEngineException($"Field not found in document [{condition.Right.Value}].");
                }

                var singleConditionResult = Condition.IsMatch(jLeftToken.ToString().ToLower(), condition.LogicalQualifier, jRightToken.ToString());

                expression.Parameters[condition.ConditionKey] = singleConditionResult;
            }
        }

        /// <summary>
        /// Gets the values of all selected fields from document.
        /// </summary>
        /// 
        private static void FillInSchemaResultDocumentValues(LookupThreadParam param, string schemaKey,
            DocumentPointer documentPointer, ref SchemaIntersectionRow schemaResultRow, Dictionary<string, JObject> threadScopedContentCache)
        {
            var jObject = threadScopedContentCache[$"{schemaKey}:{documentPointer.Key}"];

            if (param.Query.DynamicallyBuildSelectList) //The script is a "SELECT *". This is not optimal, but neither is select *...
            {
                var fields = new List<PrefixedField>();
                foreach (var jField in jObject)
                {
                    fields.Add(new PrefixedField(schemaKey, jField.Key, jField.Key));
                }

                lock (param.Query.SelectFields)
                {
                    foreach (var field in fields)
                    {
                        if (param.Query.SelectFields.OfType<FunctionDocumentFieldParameter>().Any(o => o.Value.Key == field.Key) == false)
                        {
                            var newField = new FunctionDocumentFieldParameter(field.Key)
                            {
                                Alias = field.Alias
                            };
                            param.Query.SelectFields.Add(newField);
                        }
                    }
                }
            }

            //Keep track of which schemas we've matched on.
            schemaResultRow.SchemaKeys.Add(schemaKey);

            if (schemaKey != string.Empty)
            {
                //Grab all of the selected fields from the document.
                foreach (var selectField in param.Query.SelectFields.OfType<FunctionDocumentFieldParameter>().Where(o => o.Value.Prefix == schemaKey))
                {
                    if (jObject.TryGetValue(selectField.Value.Field, StringComparison.CurrentCultureIgnoreCase, out JToken? token) == false)
                    {
                        //Field was not found, log warning which can be returned to the user.
                    }
                    schemaResultRow.InsertValue(selectField.Value.Field, selectField.Ordinal, token?.ToString());
                }
            }

            foreach (var selectField in param.Query.SelectFields.OfType<FunctionDocumentFieldParameter>().Where(o => o.Value.Prefix == string.Empty))
            {
                if (jObject.TryGetValue(selectField.Value.Field, StringComparison.CurrentCultureIgnoreCase, out JToken? token) == false)
                {
                    //Field was not found, log warning which can be returned to the user.
                }
                schemaResultRow.InsertValue(selectField.Value.Field, selectField.Ordinal, token?.ToString());
            }

            //We have to make sure that we have all of the method fields too so we can use them for calling functions.
            foreach (var methodField in param.Query.SelectFields.AllDocumentFields().Where(o => o.Prefix == schemaKey).Distinct())
            {
                if (jObject.TryGetValue(methodField.Field, StringComparison.CurrentCultureIgnoreCase, out JToken? token) == false)
                {
                    //Field was not found, log warning which can be returned to the user.
                    //throw new KbEngineException($"Method field not found: {methodField.Key}.");
                }
                schemaResultRow.MethodFields.Add(methodField.Key, token?.ToString());
            }

            //We have to make sure that we have all of the method fields too so we can use them for calling functions.
            foreach (var methodField in param.Query.GroupFields.AllDocumentFields().Where(o => o.Prefix == schemaKey).Distinct())
            {
                if (jObject.TryGetValue(methodField.Field, StringComparison.CurrentCultureIgnoreCase, out JToken? token) == false)
                {
                    //Field was not found, log warning which can be returned to the user.
                    //throw new KbEngineException($"Method field not found: {methodField.Key}.");
                }
                //schemaResultRow.MethodFields.Add(methodField.Key, token?.ToString());
            }

            schemaResultRow.MethodFields.Add($"{schemaKey}.$UID$", documentPointer.Key);

            //We have to make sure that we have all of the condition fields too so we can filter on them.
            //TODO: We could grab some of these from the field selector above to cut down on redundant json scanning.
            foreach (var conditionField in param.Query.Conditions.AllFields.Where(o => o.Prefix == schemaKey).Distinct())
            {
                if (jObject.TryGetValue(conditionField.Field, StringComparison.CurrentCultureIgnoreCase, out JToken? token) == false)
                {
                    //Field was not found, log warning which can be returned to the user.
                    //throw new KbEngineException($"Condition field not found: {conditionField.Key}.");
                }
                schemaResultRow.ConditionFields.Add(conditionField.Key, token?.ToString());
            }
        }

        #endregion

        #region WHERE clasue.

        /// <summary>
        /// This is where we filter the results by the WHERE clause.
        /// </summary>
        private static List<SchemaIntersectionRow> ApplyQueryGlobalConditions(LookupThreadParam param, SchemaIntersectionRowCollection inputResults)
        {
            var outputResults = new List<SchemaIntersectionRow>();
            var expression = new NCalc.Expression(param.Query.Conditions.HighLevelExpressionTree);

            foreach (var inputResult in inputResults.Collection)
            {
                SetQueryGlobalConditionsExpressionParameters(ref expression, param.Query.Conditions, inputResult.ConditionFields);

                var ptEvaluate = param.Transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Evaluate);
                bool evaluation = (bool)expression.Evaluate();
                ptEvaluate?.StopAndAccumulate();

                if (evaluation)
                {
                    outputResults.Add(inputResult);
                }
            }

            return outputResults;
        }

        /// <summary>
        /// Sets the parameters for the WHERE clasue expression evaluation from the condition field values saved from the MSQ lookup.
        /// </summary>
        private static void SetQueryGlobalConditionsExpressionParameters(
            ref NCalc.Expression expression, Conditions conditions, Dictionary<string, string?> conditionField)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditions.Root.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetQueryGlobalConditionsExpressionParameters(ref expression, conditions, subExpression, conditionField);
            }
        }

        /// <summary>
        /// Sets the parameters for the WHERE clasue expression evaluation from the condition field values saved from the MSQ lookup.
        /// </summary>
        private static void SetQueryGlobalConditionsExpressionParameters(ref NCalc.Expression expression,
            Conditions conditions, ConditionSubset conditionSubset, Dictionary<string, string?> conditionField)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetQueryGlobalConditionsExpressionParameters(ref expression, conditions, subExpression, conditionField);
            }

            foreach (var condition in conditionSubset.Conditions)
            {
                KbUtility.EnsureNotNull(condition.Left.Value);

                //Get the value of the condition:
                if (conditionField.TryGetValue(condition.Left.Key, out string? value) == false)
                {
                    //Field was not found, log warning which can be returned to the user.
                    //throw new KbEngineException($"Field not found in document [{condition.Left.Key}].");
                }

                expression.Parameters[condition.ConditionKey] = condition.IsMatch(value?.ToLower());
            }
        }

        #endregion
    }
}
