using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Documents;
using Katzebase.Engine.Functions.Aggregate;
using Katzebase.Engine.Functions.Parameters;
using Katzebase.Engine.Functions.Scaler;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Query.Searchers.Intersection;
using Katzebase.Engine.Query.Searchers.Mapping;
using Katzebase.Engine.Query.Sorting;
using Katzebase.Engine.Threading;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Types;
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

            IEnumerable<DocumentPointer>? documentPointers = null;
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

                        var indexMatchedDocuments = core.Indexes.MatchWorkingSchemaDocuments(transaction, topLevelMap.PhysicalSchema, subset.IndexSelection, subset, topLevelMap.Prefix);
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

            if (documentPointers == null)
            {
                documentPointers = core.Documents.AcquireDocumentPointers(transaction, topLevelMap.PhysicalSchema, LockOperation.Read);
            }

            var ptThreadCreation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCreation);
            var threadParam = new LookupThreadParam(core, transaction, schemaMap, query, gatherDocumentPointersForSchemaPrefix);
            int threadCount = ThreadPoolHelper.CalculateThreadCount(core, transaction, documentPointers.Count());

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

                //We cant stop when we hit the row limit if we are sorting or grouping.
                if (query.RowLimit != 0 && query.SortFields.Any() == false
                     && query.GroupFields.Any() == false && threadParam.Results.Collection.Count >= query.RowLimit)
                {
                    break;
                }

                var ptThreadQueue = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadQueue);
                threadPool.EnqueueWorkItem(documentPointer);
                ptThreadQueue?.StopAndAccumulate();
            }

            var ptThreadCompletion = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCompletion);
            threadPool.WaitForCompletion();
            ptThreadCompletion?.StopAndAccumulate();

            #region Grouping.

            if (threadParam.Results.Collection.Any() && (query.GroupFields.Any()
                || query.SelectFields.OfType<FunctionWithParams>().Where(o => o.FunctionType == FunctionParameterTypes.FunctionType.Aggregate).Any())
               )
            {
                IEnumerable<IGrouping<string, SchemaIntersectionRow>>? groupedValues;

                if (query.GroupFields.Any())
                {
                    //Here we are going to build a grouping_key using the concatenated select fields.
                    groupedValues = threadParam.Results.Collection.GroupBy(arr =>
                        string.Join("\t", query.GroupFields.OfType<FunctionDocumentFieldParameter>().Select(groupFieldParam => arr.AuxiliaryFields[groupFieldParam.Value.Key])));
                }
                else
                {
                    //We do not have a group by, but we do have aggregate functions. Group by an empty string.
                    groupedValues = threadParam.Results.Collection.GroupBy(arr =>
                        string.Join("\t", query.GroupFields.OfType<FunctionDocumentFieldParameter>().Select(groupFieldParam => string.Empty)));
                }

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
                            var specificValue = group.First().AuxiliaryFields[specific.Value.Key];
                            values.Add(specificValue);
                        }
                        else if (field is FunctionWithParams)
                        {
                            var proc = (FunctionWithParams)field;
                            var value = AggregateFunctionImplementation.CollapseAllFunctionParameters(proc, group);
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

                threadParam.Results = groupedResults;
            }

            #endregion

            #region Sorting.

            //Get a list of all the fields we need to sory by.
            if (query.SortFields.Any() && threadParam.Results.Collection.Any())
            {
                var modelAuxiliaryFields = threadParam.Results.Collection.First().AuxiliaryFields;

                var sortingColumns = new List<(string fieldName, KbSortDirection sortDirection)>();
                foreach (var sortField in query.SortFields.OfType<SortField>())
                {
                    if (modelAuxiliaryFields.ContainsKey(sortField.Key) == false)
                    {
                        throw new KbEngineException($"Sort field '{sortField.Alias}' was not found.");
                    }
                    sortingColumns.Add(new(sortField.Key, sortField.SortDirection));
                }

                //Sort the results:
                var ptSorting = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Sorting);
                threadParam.Results.Collection = threadParam.Results.Collection.OrderBy(row => row.AuxiliaryFields, new ResultValueComparer(sortingColumns)).ToList();

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

            var results = new DocumentLookupResults()
            {
                DocumentPointers = threadParam.DocumentPointers
            };

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

                var ptThreadDeQueue = param.Transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadDeQueue);
                var toplevelDocument = pool.DequeueWorkItem();
                ptThreadDeQueue?.StopAndAccumulate();

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
                if (param.Query.DynamicallyBuildSelectList) //The script is a "SELECT *". This is not optimal, but neither is select *...
                {
                    lock (param.Query.SelectFields) //We only have to lock this is we are dynamically building the select list.
                    {
                        ExecuteFunctions(param.Transaction, param, resultingRows);
                    }
                }
                else
                {
                    ExecuteFunctions(param.Transaction, param, resultingRows);
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

        static void ExecuteFunctions(Transaction transaction, LookupThreadParam param, SchemaIntersectionRowCollection resultingRows)
        {
            foreach (var methodField in param.Query.SelectFields.OfType<FunctionWithParams>().Where(o => o.FunctionType == FunctionParameterTypes.FunctionType.Scaler))
            {
                foreach (var row in resultingRows.Collection)
                {
                    var methodResult = ScalerFunctionImplementation.CollapseAllFunctionParameters(transaction, methodField, row.AuxiliaryFields);
                    row.InsertValue(methodField.Alias, methodField.Ordinal, methodResult);

                    //Lets make the method results available for sorting, grouping, etc.
                    var field = param.Query.SortFields.Where(o => o.Field == methodField.Alias).SingleOrDefault();
                    if (field != null && row.AuxiliaryFields.ContainsKey(field.Alias) == false)
                    {
                        row.AuxiliaryFields.Add(field.Alias, methodResult);
                    }
                }
            }

            foreach (var methodField in param.Query.SelectFields.OfType<FunctionExpression>())
            {
                foreach (var row in resultingRows.Collection)
                {
                    var methodResult = ScalerFunctionImplementation.CollapseAllFunctionParameters(transaction, methodField, row.AuxiliaryFields);
                    row.InsertValue(methodField.Alias, methodField.Ordinal, methodResult);

                    //Lets make the method results available for sorting, grouping, etc.
                    var field = param.Query.SortFields.Where(o => o.Alias == methodField.Alias).SingleOrDefault();
                    if (field != null && row.AuxiliaryFields.ContainsKey(field.Alias) == false)
                    {
                        row.AuxiliaryFields.Add(field.Alias, methodResult);
                    }
                }
            }
        }

        #endregion

        private static void IntersectAllSchemas(LookupThreadParam param, DocumentPointer topLevelDocumentPointer, ref SchemaIntersectionRowCollection resultingRows)
        {
            var topLevelSchemaMap = param.SchemaMap.First();

            var toplevelPhysicalDocument = param.Core.Documents.AcquireDocument(param.Transaction, topLevelSchemaMap.Value.PhysicalSchema, topLevelDocumentPointer, LockOperation.Read);

            var threadScopedContentCache = new KbInsensitiveDictionary<KbInsensitiveDictionary<string?>>
            {
                { $"{topLevelSchemaMap.Key.ToLower()}:{topLevelDocumentPointer.Key.ToLower()}", toplevelPhysicalDocument.Elements }
            };

            //This cache is used to store the content of all documents required for a single row join.
            var joinScopedContentCache = new KbInsensitiveDictionary<KbInsensitiveDictionary<string?>>
            {
                { topLevelSchemaMap.Key.ToLower(), toplevelPhysicalDocument.Elements }
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
                resultingRows.Collection = ApplyQueryGlobalConditions(param.Transaction, param, resultingRows);
            }
        }

        private static void IntersectAllSchemasRecursive(LookupThreadParam param,
            int skipSchemaCount, ref SchemaIntersectionRow resultingRow, ref SchemaIntersectionRowCollection resultingRows,
            ref KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> threadScopedContentCache,
            ref KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> joinScopedContentCache)
        {
            var currentSchemaKVP = param.SchemaMap.Skip(skipSchemaCount).First();
            var currentSchemaMap = currentSchemaKVP.Value;

            KbUtility.EnsureNotNull(currentSchemaMap?.Conditions);

            var expression = new NCalc.Expression(currentSchemaMap.Conditions.HighLevelExpressionTree);

            //Create a reference to the entire document catalog.
            IEnumerable<DocumentPointer>? limitedDocumentPointers = null;

            #region Indexing to reduce the number of document pointers in "limitedDocumentPointers".

            if (currentSchemaMap.Optimization?.CanApplyIndexing() == true)
            {
                //We are going to create a limited document catalog from the indexes. So kill the reference and create an empty list.
                var furtherLimitedDocumentPointers = new List<DocumentPointer>();

                //All condition subsets have a selected index. Start building a list of possible document IDs.
                foreach (var subset in currentSchemaMap.Optimization.Conditions.NonRootSubsets)
                {
                    KbUtility.EnsureNotNull(subset.IndexSelection);

                    var keyValuePairs = new KbInsensitiveDictionary<string>();

                    //Grab the values from the schema above and save them for the index lookup of the next schema in the join.
                    foreach (var condition in subset.Conditions)
                    {
                        var documentContent = joinScopedContentCache[condition.Right?.Prefix ?? ""];

                        if (!documentContent.TryGetValue(condition.Right?.Value ?? "", out string? documentValue))
                        {
                            throw new KbEngineException($"Join clause field not found in document [{currentSchemaKVP.Key}].");
                        }
                        keyValuePairs.Add(condition.Left?.Value ?? "", documentValue?.ToString() ?? "");
                    }

                    //Match on values from the document.
                    var documentIds = param.Core.Indexes.MatchConditionValuesDocuments(param.Transaction, currentSchemaMap.PhysicalSchema, subset.IndexSelection, subset, keyValuePairs);

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

            if (limitedDocumentPointers == null)
            {
                limitedDocumentPointers = param.Core.Documents.AcquireDocumentPointers(param.Transaction, currentSchemaMap.PhysicalSchema, LockOperation.Read);
            }

            #endregion

            int matchesFromThisSchema = 0;

            var rowTemplate = resultingRow.Clone();

            foreach (var documentPointer in limitedDocumentPointers)
            {
                string threadScopeddocumentCacheKey = $"{currentSchemaKVP.Key}:{documentPointer.Key}";

                //Get the document content from the thread cache (or cache it).
                if (threadScopedContentCache.TryGetValue(threadScopeddocumentCacheKey, out var documentContentNextLevel) == false)
                {
                    var physicalDocumentNextLevel = param.Core.Documents.AcquireDocument(param.Transaction, currentSchemaMap.PhysicalSchema, documentPointer, LockOperation.Read);
                    documentContentNextLevel = physicalDocumentNextLevel.Elements;
                    threadScopedContentCache.Add(threadScopeddocumentCacheKey, documentContentNextLevel);
                }

                joinScopedContentCache.Add(currentSchemaKVP.Key.ToLower(), documentContentNextLevel);

                SetSchemaIntersectionExpressionParameters(param.Transaction, ref expression, currentSchemaMap.Conditions, joinScopedContentCache);

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
        private static void SetSchemaIntersectionExpressionParameters(Transaction transaction,
            ref NCalc.Expression expression, Conditions conditions, KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> joinScopedContentCache)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditions.Root.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetSchemaIntersectionExpressionParametersRecursive(transaction, ref expression, conditions, subExpression, joinScopedContentCache);
            }
        }

        private static void SetSchemaIntersectionExpressionParametersRecursive(Transaction transaction,
            ref NCalc.Expression expression, Conditions conditions, ConditionSubset conditionSubset,
            KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> joinScopedContentCache)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetSchemaIntersectionExpressionParametersRecursive(transaction, ref expression, conditions, subExpression, joinScopedContentCache);
            }

            foreach (var condition in conditionSubset.Conditions)
            {
                KbUtility.EnsureNotNull(condition.Left.Value);
                KbUtility.EnsureNotNull(condition.Right.Value);

                //Get the value of the condition:
                var documentContent = joinScopedContentCache[condition.Left.Prefix];
                if (!documentContent.TryGetValue(condition.Left.Value, out string? leftDocumentValue))
                {
                    throw new KbEngineException($"Field not found in document [{condition.Left.Value}].");
                }

                //Get the value of the condition:
                documentContent = joinScopedContentCache[condition.Right.Prefix];
                if (!documentContent.TryGetValue(condition.Right.Value, out string? reftDocumentValue))
                {
                    throw new KbEngineException($"Field not found in document [{condition.Right.Value}].");
                }

                var singleConditionResult = Condition.IsMatch(transaction, leftDocumentValue?.ToLower(), condition.LogicalQualifier, reftDocumentValue);

                expression.Parameters[condition.ConditionKey] = singleConditionResult;
            }
        }

        private static void FillInSchemaResultDocumentValues(LookupThreadParam param, string schemaKey,
            DocumentPointer documentPointer, ref SchemaIntersectionRow schemaResultRow, KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> threadScopedContentCache)
        {
            if (param.Query.DynamicallyBuildSelectList) //The script is a "SELECT *". This is not optimal, but neither is select *...
            {
                lock (param.Query.SelectFields) //We only have to lock this is we are dynamically building the select list.
                {
                    FillInSchemaResultDocumentValuesAtomic(param, schemaKey, documentPointer, ref schemaResultRow, threadScopedContentCache);
                }
            }
            else
            {
                FillInSchemaResultDocumentValuesAtomic(param, schemaKey, documentPointer, ref schemaResultRow, threadScopedContentCache);
            }
        }

        /// <summary>
        /// Gets the values of all selected fields from document.
        /// </summary>
        /// 
        private static void FillInSchemaResultDocumentValuesAtomic(LookupThreadParam param, string schemaKey,
            DocumentPointer documentPointer, ref SchemaIntersectionRow schemaResultRow, KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> threadScopedContentCache)
        {
            var documentContent = threadScopedContentCache[$"{schemaKey}:{documentPointer.Key}"];

            if (param.Query.DynamicallyBuildSelectList) //The script is a "SELECT *". This is not optimal, but neither is select *...
            {
                var fields = new List<PrefixedField>();
                foreach (var documentValue in documentContent)
                {
                    fields.Add(new PrefixedField(schemaKey, documentValue.Key, documentValue.Key));
                }

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

            //Keep track of which schemas we've matched on.
            schemaResultRow.SchemaKeys.Add(schemaKey);

            if (schemaKey != string.Empty)
            {
                //Grab all of the selected fields from the document.
                foreach (var field in param.Query.SelectFields.OfType<FunctionDocumentFieldParameter>().Where(o => o.Value.Prefix == schemaKey))
                {
                    if (documentContent.TryGetValue(field.Value.Field, out string? documentValue) == false)
                    {
                        param.Transaction.AddWarning(KbTransactionWarning.SelectFieldNotFound, $"'{field.Value.Field}' will be treated as null.");
                    }
                    schemaResultRow.InsertValue(field.Value.Field, field.Ordinal, documentValue);
                }
            }

            foreach (var field in param.Query.SelectFields.OfType<FunctionDocumentFieldParameter>().Where(o => o.Value.Prefix == string.Empty))
            {
                if (documentContent.TryGetValue(field.Value.Field, out string? documentValue) == false)
                {
                    param.Transaction.AddWarning(KbTransactionWarning.SelectFieldNotFound, $"'{field.Value.Field}' will be treated as null.");
                }
                schemaResultRow.InsertValue(field.Value.Field, field.Ordinal, documentValue);
            }

            schemaResultRow.AuxiliaryFields.Add($"{schemaKey}.$UID$", documentPointer.Key);

            //We have to make sure that we have all of the method fields too so we can use them for calling functions.
            foreach (var field in param.Query.SelectFields.AllDocumentFields.Where(o => o.Prefix == schemaKey).Distinct())
            {
                if (schemaResultRow.AuxiliaryFields.ContainsKey(field.Key) == false)
                {
                    if (documentContent.TryGetValue(field.Field, out string? documentValue) == false)
                    {
                        param.Transaction.AddWarning(KbTransactionWarning.MethodFieldNotFound, $"'{field.Key}' will be treated as null.");
                    }
                    schemaResultRow.AuxiliaryFields.Add(field.Key, documentValue);
                }
            }

            //We have to make sure that we have all of the method fields too so we can use them for calling functions.
            foreach (var field in param.Query.GroupFields.AllDocumentFields.Where(o => o.Prefix == schemaKey).Distinct())
            {
                if (schemaResultRow.AuxiliaryFields.ContainsKey(field.Key) == false)
                {
                    if (documentContent.TryGetValue(field.Field, out string? documentValue) == false)
                    {
                        param.Transaction.AddWarning(KbTransactionWarning.GroupFieldNotFound, $"'{field.Key}' will be treated as null.");
                    }
                    schemaResultRow.AuxiliaryFields.Add(field.Key, documentValue);
                }
            }

            //We have to make sure that we have all of the condition fields too so we can filter on them.
            foreach (var field in param.Query.Conditions.AllFields.Where(o => o.Prefix == schemaKey).Distinct())
            {
                if (schemaResultRow.AuxiliaryFields.ContainsKey(field.Key) == false)
                {
                    if (documentContent.TryGetValue(field.Field, out string? documentValue) == false)
                    {
                        param.Transaction.AddWarning(KbTransactionWarning.ConditionFieldNotFound, $"'{field.Key}' will be treated as null.");
                    }
                    schemaResultRow.AuxiliaryFields.Add(field.Key, documentValue);
                }
            }

            //We have to make sure that we have all of the sort fields too so we can filter on them.
            foreach (var field in param.Query.SortFields.Where(o => o.Prefix == schemaKey).Distinct())
            {
                if (schemaResultRow.AuxiliaryFields.ContainsKey(field.Key) == false)
                {
                    if (documentContent.TryGetValue(field.Field, out string? documentValue) == false)
                    {
                        param.Transaction.AddWarning(KbTransactionWarning.SortFieldNotFound, $"'{field.Key}' will be treated as null.");
                    }
                    schemaResultRow.AuxiliaryFields.Add(field.Key, documentValue);
                }
            }
        }

        #endregion

        #region WHERE clasue.

        /// <summary>
        /// This is where we filter the results by the WHERE clause.
        /// </summary>
        private static List<SchemaIntersectionRow> ApplyQueryGlobalConditions(Transaction transaction, LookupThreadParam param, SchemaIntersectionRowCollection inputResults)
        {
            var outputResults = new List<SchemaIntersectionRow>();
            var expression = new NCalc.Expression(param.Query.Conditions.HighLevelExpressionTree);

            foreach (var inputResult in inputResults.Collection)
            {
                SetQueryGlobalConditionsExpressionParameters(transaction, ref expression, param.Query.Conditions, inputResult.AuxiliaryFields);

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
        private static void SetQueryGlobalConditionsExpressionParameters(Transaction transaction,
            ref NCalc.Expression expression, Conditions conditions, KbInsensitiveDictionary<string?> conditionField)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditions.Root.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetQueryGlobalConditionsExpressionParameters(transaction, ref expression, conditions, subExpression, conditionField);
            }
        }

        /// <summary>
        /// Sets the parameters for the WHERE clasue expression evaluation from the condition field values saved from the MSQ lookup.
        /// </summary>
        private static void SetQueryGlobalConditionsExpressionParameters(Transaction transaction, ref NCalc.Expression expression,
            Conditions conditions, ConditionSubset conditionSubset, KbInsensitiveDictionary<string?> conditionField)
        {
            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetQueryGlobalConditionsExpressionParameters(transaction, ref expression, conditions, subExpression, conditionField);
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

                expression.Parameters[condition.ConditionKey] = condition.IsMatch(transaction, value?.ToLower());
            }
        }

        #endregion
    }
}
