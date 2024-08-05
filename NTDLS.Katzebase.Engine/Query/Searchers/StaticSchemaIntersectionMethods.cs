using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Functions.Parameters;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Query.Constraints;
using NTDLS.Katzebase.Engine.Query.Searchers.Intersection;
using NTDLS.Katzebase.Engine.Query.Searchers.Mapping;
using NTDLS.Katzebase.Engine.Query.Sorting;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Documents.DocumentPointer;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;
using static NTDLS.Katzebase.Engine.Trace.PerformanceTrace;

namespace NTDLS.Katzebase.Engine.Query.Searchers
{
    internal static class StaticSchemaIntersectionMethods
    {
        /// <summary>
        /// Build a generic key/value dataset which is the combined field-set from each inner joined document.
        /// </summary>
        /// <param name="gatherDocumentPointersForSchemaPrefix">When not null, the process will focus on
        /// obtaining a list of DocumentPointers instead of key/values. This is used for UPDATES and DELETES.</param>
        /// <returns></returns>
        internal static DocumentLookupResults GetDocumentsByConditions(EngineCore core, Transaction transaction,
            QuerySchemaMap schemaMap, PreparedQuery query, string? gatherDocumentPointersForSchemaPrefix = null)
        {
            var topLevel = schemaMap.First();
            var topLevelMap = topLevel.Value;

            IEnumerable<DocumentPointer>? documentPointers = null;
            ConditionLookupOptimization? lookupOptimization = null;

            //TODO: Here we should evaluate whatever conditions we can to early eliminate the top level document scans.
            //If we don't have any conditions then we just need to return all rows from the schema.
            if (query.Conditions.Subsets.Count > 0)
            {
                lookupOptimization = ConditionLookupOptimization.Build(
                    core, transaction, topLevelMap.PhysicalSchema, query.Conditions, topLevelMap.Prefix);

                var limitedDocumentPointers = new List<DocumentPointer>();


                if (lookupOptimization.CanApplyIndexing())
                {
                    transaction.AddMessage($"Applying {lookupOptimization.IndexSelection.Count} index(s).", KbMessageType.Verbose);

                    foreach (var index in lookupOptimization.IndexSelection)
                    {
                        var coveredFields = string.Join("', '", index.CoveredFields.Select(o => o.Key)).Trim();
                        transaction.AddMessage($"Index '{index.PhysicalIndex.Name}' covers {coveredFields}", KbMessageType.Verbose);
                    }

                    //We are going to create a limited document catalog from the indexes. So kill the reference and create an empty list.
                    documentPointers = new List<DocumentPointer>();

                    //All condition subsets have a selected index. Start building a list of possible document IDs.
                    foreach (var subset in lookupOptimization.Conditions.NonRootSubsets)
                    {
                        transaction.AddMessage($"Expression: ({subset.Expression}) {{", KbMessageType.Verbose);

                        foreach (var cond in subset.Conditions)
                        {
                            string leftIndex = string.Empty;
                            string rightIndex = string.Empty;

                            if (cond.CoveredByIndex)
                            {
                                foreach (var index in lookupOptimization.IndexSelection)
                                {
                                    if (index.CoveredFields.Any(o => o.Key == cond.Left.Key))
                                    {
                                        leftIndex = index.PhysicalIndex.Name;
                                    }
                                    if (index.CoveredFields.Any(o => o.Key == cond.Right.Key))
                                    {
                                        rightIndex = index.PhysicalIndex.Name;
                                    }
                                }
                            }

                            string leftValue = cond.Left.IsConstant ? $"'{cond.Left.Key}'" : cond.Left.Key;
                            string rightValue = cond.Right.IsConstant ? $"'{cond.Right.Key}'" : cond.Right.Key;

                            string indexInfo = string.Empty;

                            if (string.IsNullOrEmpty(leftIndex) == false || string.IsNullOrEmpty(rightIndex) == false)
                            {
                                indexInfo += ", Indexes (";

                                if (string.IsNullOrEmpty(leftIndex) == false)
                                {
                                    indexInfo += $"Left: [{leftIndex}] ";
                                }

                                if (string.IsNullOrEmpty(rightIndex) == false)
                                {
                                    indexInfo += $"Right: [{rightIndex}] ";
                                }

                                indexInfo = indexInfo.Trim();

                                indexInfo += ")";
                            }

                            transaction.AddMessage($"\t'{cond.ConditionKey}: ({leftValue} {cond.LogicalQualifier} {rightValue}){indexInfo}", KbMessageType.Verbose);
                        }
                        transaction.AddMessage("}", KbMessageType.Verbose);

                        var indexMatchedDocuments = core.Indexes.MatchWorkingSchemaDocuments
                            (transaction, topLevelMap.PhysicalSchema, subset.IndexSelection.EnsureNotNull(), subset, topLevelMap.Prefix);

                        limitedDocumentPointers.AddRange(indexMatchedDocuments.Select(o => o.Value));
                    }

                    documentPointers = limitedDocumentPointers;
                }
                else
                {
                    transaction.AddMessage("Will not use indexing.", KbMessageType.Verbose);


                    #region Why no indexing? Find out here!
                    //   * One or more of the condition subsets lacks an index.
                    //   *
                    //   *   Since indexing requires that we can ensure document elimination we will have
                    //   *      to ensure that we have a covering index on EACH-and-EVERY condition group.
                    //   *
                    //   *   Then we can search the indexes for each condition group to obtain a list of all possible
                    //   *       document IDs, then use those document IDs to early eliminate documents from the main lookup loop.
                    //   *
                    //   *   If any one condition group does not have an index, then no indexing will be used at all since all
                    //   *      documents will need to be scanned anyway. To prevent unindexed scans, reduce the number of
                    //   *      condition groups (nested in parentheses).
                    //   *
                    //   * ConditionLookupOptimization:BuildFullVirtualExpression() Will tell you why we cant use an index.
                    //   * var explanationOfIndexability = lookupOptimization.BuildFullVirtualExpression();
                    #endregion
                }
            }

            //If we do not have any documents, then get the whole schema.
            documentPointers ??= core.Documents.AcquireDocumentPointers(transaction, topLevelMap.PhysicalSchema, LockOperation.Read);

            var threadPoolQueue = core.ThreadPool.Generic.CreateQueueStateTracker();

            var operation = new LookupThreadOperation(core, transaction, schemaMap, query, gatherDocumentPointersForSchemaPrefix);

            foreach (var documentPointer in documentPointers)
            {
                //We cant stop when we hit the row limit if we are sorting or grouping.
                if (query.RowLimit != 0 && query.SortFields.Any() == false
                     && query.GroupFields.Any() == false && operation.Results.Collection.Count >= query.RowLimit)
                {
                    break;
                }

                if (threadPoolQueue.ExceptionOccurred())
                {
                    break;
                }

                var instance = new LookupThreadInstance(operation, documentPointer);

                var ptThreadQueue = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadQueue);
                threadPoolQueue.Enqueue(instance, LookupThreadWorker);
                ptThreadQueue?.StopAndAccumulate();
            }

            var ptThreadCompletion = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCompletion);
            threadPoolQueue.WaitForCompletion();
            ptThreadCompletion?.StopAndAccumulate();

            #region Grouping.

            if (operation.Results.Collection.Count != 0 && (query.GroupFields.Count != 0
                || query.SelectFields.OfType<FunctionWithParams>()
                .Any(o => o.FunctionType == FunctionParameterTypes.FunctionType.Aggregate))
               )
            {
                IEnumerable<IGrouping<string, SchemaIntersectionRow>>? groupedValues;

                if (query.GroupFields.Any())
                {
                    //Here we are going to build a grouping_key using the concatenated select fields.
                    groupedValues = operation.Results.Collection.GroupBy(arr =>
                        string.Join('\t', query.GroupFields.OfType<FunctionDocumentFieldParameter>()
                        .Select(groupFieldParam => arr.AuxiliaryFields[groupFieldParam.Value.Key])));
                }
                else
                {
                    //We do not have a group by, but we do have aggregate functions. Group by an empty string.
                    groupedValues = operation.Results.Collection.GroupBy(arr =>
                        string.Join('\t', query.GroupFields.OfType<FunctionDocumentFieldParameter>().Select(groupFieldParam => string.Empty)));
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

                operation.Results = groupedResults;
            }

            #endregion

            #region Sorting.

            //Get a list of all the fields we need to sort by.
            if (query.SortFields.Any() && operation.Results.Collection.Any())
            {
                var modelAuxiliaryFields = operation.Results.Collection.First().AuxiliaryFields;

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
                operation.Results.Collection = operation.Results.Collection.OrderBy
                    (row => row.AuxiliaryFields, new ResultValueComparer(sortingColumns)).ToList();

                ptSorting?.StopAndAccumulate();
            }

            #endregion

            //Enforce row limits.
            if (query.RowLimit > 0)
            {
                operation.Results.Collection = operation.Results.Collection.Take(query.RowLimit).ToList();
            }

            if (gatherDocumentPointersForSchemaPrefix != null)
            {
                //Distill the document pointers to a distinct list. Can we do this in the threads? Maybe prevent the dups in the first place?
                operation.DocumentPointers = operation.DocumentPointers.Distinct(new DocumentPageEqualityComparer()).ToList();
            }

            if (query.DynamicallyBuildSelectList && operation.Results.Collection.Count > 0)
            {
                //If this was a "select *", we may have discovered different "fields" in different 
                //  documents. We need to make sure that all rows have the same number of values.

                int maxFieldCount = query.SelectFields.Count;
                foreach (var row in operation.Results.Collection)
                {
                    if (row.Values.Count < maxFieldCount)
                    {
                        int difference = maxFieldCount - row.Values.Count;
                        row.Values.AddRange(new string[difference]);
                    }
                }
            }

            var results = new DocumentLookupResults();

            results.DocumentPointers.AddRange(operation.DocumentPointers);

            results.AddRange(operation.Results);

            return results;
        }

        #region Threading.

        private static void LookupThreadWorker(object? parameter)
        {
            var instance = parameter.EnsureNotNull<LookupThreadInstance>();

            instance.Operation.Transaction.EnsureActive();

            var resultingRows = new SchemaIntersectionRowCollection();

            IntersectAllSchemas(instance, instance.DocumentPointer, ref resultingRows);

            //Limit the results by the rows that have the correct number of schema matches.
            //TODO: This could probably be used to implement OUTER JOINS.
            if (instance.Operation.GatherDocumentPointersForSchemaPrefix == null)
            {
                resultingRows.Collection = resultingRows.Collection.Where(o => o.SchemaKeys.Count == instance.Operation.SchemaMap.Count).ToList();
            }
            else
            {
                resultingRows.Collection = resultingRows.Collection.Where(o => o.SchemaDocumentPointers.Count == instance.Operation.SchemaMap.Count).ToList();
            }

            //Execute functions
            if (instance.Operation.Query.DynamicallyBuildSelectList) //The script is a "SELECT *". This is not optimal, but neither is select *...
            {
                lock (instance.Operation.Query.SelectFields) //We only have to lock this is we are dynamically building the select list.
                {
                    ExecuteFunctions(instance.Operation.Transaction, instance.Operation, resultingRows);
                }
            }
            else
            {
                ExecuteFunctions(instance.Operation.Transaction, instance.Operation, resultingRows);
            }

            lock (instance.Operation.Results)
            {
                //Accumulate the results up to the parent.
                if (instance.Operation.GatherDocumentPointersForSchemaPrefix == null)
                {
                    instance.Operation.Results.AddRange(resultingRows);
                }
                else
                {
                    instance.Operation.DocumentPointers.AddRange(resultingRows.Collection.Select(o => o.SchemaDocumentPointers[instance.Operation.GatherDocumentPointersForSchemaPrefix]));
                }
            }
        }

        static void ExecuteFunctions(Transaction transaction, LookupThreadOperation operation, SchemaIntersectionRowCollection resultingRows)
        {
            foreach (var methodField in operation.Query.SelectFields.OfType<FunctionWithParams>().Where(o => o.FunctionType == FunctionParameterTypes.FunctionType.Scaler))
            {
                foreach (var row in resultingRows.Collection)
                {
                    var methodResult = ScalerFunctionImplementation.CollapseAllFunctionParameters(transaction, methodField, row.AuxiliaryFields);
                    row.InsertValue(methodField.Alias, methodField.Ordinal, methodResult);

                    //Lets make the method results available for sorting, grouping, etc.
                    var field = operation.Query.SortFields.SingleOrDefault(o => o.Field == methodField.Alias);
                    if (field != null && row.AuxiliaryFields.ContainsKey(field.Alias) == false)
                    {
                        row.AuxiliaryFields.Add(field.Alias, methodResult);
                    }
                }
            }

            foreach (var methodField in operation.Query.SelectFields.OfType<FunctionExpression>())
            {
                foreach (var row in resultingRows.Collection)
                {
                    var methodResult = ScalerFunctionImplementation.CollapseAllFunctionParameters(transaction, methodField, row.AuxiliaryFields);
                    row.InsertValue(methodField.Alias, methodField.Ordinal, methodResult);

                    //Lets make the method results available for sorting, grouping, etc.
                    var field = operation.Query.SortFields.SingleOrDefault(o => o.Alias == methodField.Alias);
                    if (field != null && row.AuxiliaryFields.ContainsKey(field.Alias) == false)
                    {
                        row.AuxiliaryFields.Add(field.Alias, methodResult);
                    }
                }
            }
        }

        #endregion

        private static void IntersectAllSchemas(LookupThreadInstance instance,
            DocumentPointer topLevelDocumentPointer, ref SchemaIntersectionRowCollection resultingRows)
        {
            var topLevelSchemaMap = instance.Operation.SchemaMap.First();

            var topLevelPhysicalDocument = instance.Operation.Core.Documents.AcquireDocument
                (instance.Operation.Transaction, topLevelSchemaMap.Value.PhysicalSchema, topLevelDocumentPointer, LockOperation.Read);

            var threadScopedContentCache = new KbInsensitiveDictionary<KbInsensitiveDictionary<string?>>
            {
                { $"{topLevelSchemaMap.Key.ToLowerInvariant()}:{topLevelDocumentPointer.Key.ToLowerInvariant()}", topLevelPhysicalDocument.Elements }
            };

            //This cache is used to store the content of all documents required for a single row join.
            var joinScopedContentCache = new KbInsensitiveDictionary<KbInsensitiveDictionary<string?>>
            {
                { topLevelSchemaMap.Key.ToLowerInvariant(), topLevelPhysicalDocument.Elements }
            };

            var resultingRow = new SchemaIntersectionRow();
            lock (resultingRows)
            {
                resultingRows.Add(resultingRow);
            }

            if (instance.Operation.GatherDocumentPointersForSchemaPrefix != null)
            {
                resultingRow.AddSchemaDocumentPointer(topLevelSchemaMap.Key, topLevelDocumentPointer);
            }

            FillInSchemaResultDocumentValues(instance, topLevelSchemaMap.Key,
                topLevelDocumentPointer, ref resultingRow, threadScopedContentCache);

            //Since FillInSchemaResultDocumentValues() will produce a single row, this is where we can fill
            //  in any of the constant values. Additionally, this is the "template row" that will be cloned
            //  for rows produced by any one-to-many relationships.
            //
            foreach (var field in instance.Operation.Query.SelectFields.OfType<FunctionConstantParameter>())
            {
                resultingRow.InsertValue(field.Alias, field.Ordinal, field.FinalValue);
            }

            if (instance.Operation.SchemaMap.Count > 1)
            {
                IntersectAllSchemasRecursive(instance, 1, ref resultingRow,
                    ref resultingRows, ref threadScopedContentCache, ref joinScopedContentCache);
            }

            if (instance.Operation.Query.Conditions.AllFields.Any())
            {
                //Filter the rows by the global conditions.
                resultingRows.Collection = ApplyQueryGlobalConditions(instance.Operation.Transaction, instance, resultingRows);
            }
        }


        /// <summary>
        /// This function is designed to handle one-to-one and one-to-many, so it can produce more than one row.
        /// </summary>
        /// <param name="instance">Thread state</param>
        /// <param name="skipSchemaCount">The number of schemas to skip. This is the current recursion depth starting at 1.</param>
        /// <param name="resultingRow">The row reference from the parent call, which is either the top level call or a recursive call.
        ///                                 This is both populated by the recursion and used as a row tempate for one-to-many relationships.</param>
        /// <param name="resultingRows">The buffer containing all of the rows which have been found.</param>
        /// <param name="threadScopedContentCache">Document cache for the lifetime of the entire join operation for this thread.</param>
        /// <param name="joinScopedContentCache">>Document cache used the lifetime of a single row join for this thread.</param>
        /// <exception cref="KbEngineException"></exception>
        private static void IntersectAllSchemasRecursive(LookupThreadInstance instance,
            int skipSchemaCount, ref SchemaIntersectionRow resultingRow, ref SchemaIntersectionRowCollection resultingRows,
            ref KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> threadScopedContentCache,
            ref KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> joinScopedContentCache)
        {
            var currentSchemaKVP = instance.Operation.SchemaMap.Skip(skipSchemaCount).First();
            var currentSchemaMap = currentSchemaKVP.Value;

            var expressionHash = currentSchemaMap.Conditions.EnsureNotNull().Hash
                ?? throw new KbEngineException($"The expression hash cannot be null.");

            NCalc.Expression? expression;

            lock (instance.ExpressionCache)
            {
                if (instance.ExpressionCache.TryGetValue(expressionHash, out expression) == false)
                {
                    expression = new NCalc.Expression(currentSchemaMap.Conditions.EnsureNotNull().HighLevelExpressionTree);
                    instance.ExpressionCache.Add(expressionHash, expression);
                }
            }

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
                    var documentIds = instance.Operation.Core.Indexes.MatchConditionValuesDocuments
                        (instance.Operation.Transaction, currentSchemaMap.PhysicalSchema, subset.IndexSelection.EnsureNotNull(), subset, keyValuePairs);

                    furtherLimitedDocumentPointers.AddRange(documentIds.Values);
                }

                limitedDocumentPointers = furtherLimitedDocumentPointers;
            }
            else
            {
                #region Why no indexing? Find out here!
                //   * One or more of the condition subsets lacks an index.
                //   *
                //   *   Since indexing requires that we can ensure document elimination we will have
                //   *      to ensure that we have a covering index on EACH-and-EVERY condition group.
                //   *
                //   *   Then we can search the indexes for each condition group to obtain a list of all possible
                //   *       document IDs, then use those document IDs to early eliminate documents from the main lookup loop.
                //   *
                //   *   If any one condition group does not have an index, then no indexing will be used at all since all
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
                limitedDocumentPointers = instance.Operation.Core.Documents.AcquireDocumentPointers(
                    instance.Operation.Transaction, currentSchemaMap.PhysicalSchema, LockOperation.Read);
            }

            #endregion

            int matchesFromThisSchema = 0;

            //Keep a copy of the row as it was at this level of recursion. If we find a one-to-many
            //  relationship then we will need to use this to make additional copies of the original row.
            var rowTemplate = resultingRow.Clone();

            foreach (var documentPointer in limitedDocumentPointers)
            {
                string threadScopedDocumentCacheKey = $"{currentSchemaKVP.Key}:{documentPointer.Key}";

                //Get the document content from the thread cache (or cache it).
                if (threadScopedContentCache.TryGetValue(threadScopedDocumentCacheKey, out var documentContentNextLevel) == false)
                {
                    var physicalDocumentNextLevel = instance.Operation.Core.Documents.AcquireDocument(
                        instance.Operation.Transaction, currentSchemaMap.PhysicalSchema, documentPointer, LockOperation.Read);

                    documentContentNextLevel = physicalDocumentNextLevel.Elements;
                    threadScopedContentCache.Add(threadScopedDocumentCacheKey, documentContentNextLevel);
                }

                joinScopedContentCache.Add(currentSchemaKVP.Key.ToLowerInvariant(), documentContentNextLevel);

                SetSchemaIntersectionExpressionParameters(instance.Operation.Transaction,
                    ref expression, currentSchemaMap.Conditions, joinScopedContentCache);

                var ptEvaluate = instance.Operation.Transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Evaluate);
                bool evaluation = (bool)expression.Evaluate();
                ptEvaluate?.StopAndAccumulate();

                if (evaluation)
                {
                    matchesFromThisSchema++;

                    if (matchesFromThisSchema > 1)
                    {
                        //If durring this level of recursion, we found more than one match then we are working with a one-to-many.
                        //This means that we will take the a copy of the original partially populated row, and create yet another
                        //  copy of it. This copy will become our new working row which we will pass recursively down for popualtion
                        //  with and and all other joins.

                        resultingRow = rowTemplate.Clone();
                        lock (resultingRows)
                        {
                            resultingRows.Add(resultingRow);
                        }
                    }

                    if (instance.Operation.GatherDocumentPointersForSchemaPrefix != null)
                    {
                        resultingRow.AddSchemaDocumentPointer(currentSchemaKVP.Key, documentPointer);
                    }

                    //We fill in the values for the single working row: resultingRow
                    FillInSchemaResultDocumentValues(instance, currentSchemaKVP.Key, documentPointer, ref resultingRow, threadScopedContentCache);

                    if (skipSchemaCount < instance.Operation.SchemaMap.Count - 1)
                    {
                        //We continue to recursively fill in the values for the single working row: resultingRow
                        //Note that "resultingRow" is a reference, but in the case of a one-to-many, then it is a reference to the resultingRow clone.
                        IntersectAllSchemasRecursive(instance, skipSchemaCount + 1,
                            ref resultingRow, ref resultingRows, ref threadScopedContentCache, ref joinScopedContentCache);
                    }
                }

                joinScopedContentCache.Remove(currentSchemaKVP.Key); //We are no longer working with the document at this level.
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
            ref NCalc.Expression expression, Conditions conditions,
            KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> joinScopedContentCache)
        {
            //If we have subsets, then we need to satisfy those in order to complete the equation.
            foreach (var subsetKey in conditions.Root.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetSchemaIntersectionExpressionParametersRecursive(transaction,
                    ref expression, conditions, subExpression, joinScopedContentCache);
            }
        }

        private static void SetSchemaIntersectionExpressionParametersRecursive(Transaction transaction,
            ref NCalc.Expression expression, Conditions conditions, ConditionSubset conditionSubset,
            KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> joinScopedContentCache)
        {
            //If we have subsets, then we need to satisfy those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetSchemaIntersectionExpressionParametersRecursive(transaction,
                    ref expression, conditions, subExpression, joinScopedContentCache);
            }

            foreach (var condition in conditionSubset.Conditions)
            {
                //Get the value of the condition:
                var documentContent = joinScopedContentCache[condition.Left.Prefix];
                if (!documentContent.TryGetValue(condition.Left.Value.EnsureNotNull(), out string? leftDocumentValue))
                {
                    throw new KbEngineException($"Field not found in document [{condition.Left.Value}].");
                }

                //Get the value of the condition:
                documentContent = joinScopedContentCache[condition.Right.Prefix];
                if (!documentContent.TryGetValue(condition.Right.Value.EnsureNotNull(), out string? rightDocumentValue))
                {
                    throw new KbEngineException($"Field not found in document [{condition.Right.Value}].");
                }

                var singleConditionResult = Condition.IsMatch(transaction,
                    leftDocumentValue?.ToLowerInvariant(), condition.LogicalQualifier, rightDocumentValue);

                expression.Parameters[condition.ConditionKey] = singleConditionResult;
            }
        }

        /// <summary>
        /// This function will "produce" a single row.
        /// </summary>
        private static void FillInSchemaResultDocumentValues(LookupThreadInstance instance, string schemaKey,
            DocumentPointer documentPointer, ref SchemaIntersectionRow schemaResultRow,
            KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> threadScopedContentCache)
        {
            if (instance.Operation.Query.DynamicallyBuildSelectList) //The script is a "SELECT *". This is not optimal, but neither is select *...
            {
                lock (instance.Operation.Query.SelectFields) //We only have to lock this is we are dynamically building the select list.
                {
                    FillInSchemaResultDocumentValuesAtomic(instance, schemaKey, documentPointer, ref schemaResultRow, threadScopedContentCache);
                }
            }
            else
            {
                FillInSchemaResultDocumentValuesAtomic(instance, schemaKey, documentPointer, ref schemaResultRow, threadScopedContentCache);
            }
        }

        /// <summary>
        /// Gets the values of all selected fields from document.
        /// </summary>
        /// 
        private static void FillInSchemaResultDocumentValuesAtomic(LookupThreadInstance instance, string schemaKey,
            DocumentPointer documentPointer, ref SchemaIntersectionRow schemaResultRow,
            KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> threadScopedContentCache)
        {
            var documentContent = threadScopedContentCache[$"{schemaKey}:{documentPointer.Key}"];

            if (instance.Operation.Query.DynamicallyBuildSelectList) //The script is a "SELECT *". This is not optimal, but neither is select *...
            {
                var fields = new List<PrefixedField>();
                foreach (var documentValue in documentContent)
                {
                    fields.Add(new PrefixedField(schemaKey, documentValue.Key, documentValue.Key));
                }

                foreach (var field in fields)
                {
                    if (instance.Operation.Query.SelectFields.OfType<FunctionDocumentFieldParameter>().Any(o => o.Value.Key == field.Key) == false)
                    {
                        var newField = new FunctionDocumentFieldParameter(field.Key)
                        {
                            Alias = field.Alias
                        };
                        instance.Operation.Query.SelectFields.Add(newField);
                    }
                }
            }

            //Keep track of which schemas we've matched on.
            schemaResultRow.SchemaKeys.Add(schemaKey);

            if (schemaKey != string.Empty)
            {
                //Grab all of the selected fields from the document.
                foreach (var field in instance.Operation.Query.SelectFields.OfType<FunctionDocumentFieldParameter>().Where(o => o.Value.Prefix == schemaKey))
                {
                    if (documentContent.TryGetValue(field.Value.Field, out string? documentValue) == false)
                    {
                        instance.Operation.Transaction.AddWarning(KbTransactionWarning.SelectFieldNotFound,
                            $"'{field.Value.Field}' will be treated as null.");
                    }
                    schemaResultRow.InsertValue(field.Value.Field, field.Ordinal, documentValue);
                }
            }

            foreach (var field in instance.Operation.Query.SelectFields.OfType<FunctionDocumentFieldParameter>().Where(o => o.Value.Prefix == string.Empty))
            {
                if (documentContent.TryGetValue(field.Value.Field, out string? documentValue) == false)
                {
                    instance.Operation.Transaction.AddWarning(KbTransactionWarning.SelectFieldNotFound,
                        $"'{field.Value.Field}' will be treated as null.");
                }
                schemaResultRow.InsertValue(field.Value.Field, field.Ordinal, documentValue);
            }

            schemaResultRow.AuxiliaryFields.Add($"{schemaKey}.{UIDMarker}", documentPointer.Key);

            //We have to make sure that we have all of the method fields too so we can use them for calling functions.
            foreach (var field in instance.Operation.Query.SelectFields.AllDocumentFields.Where(o => o.Prefix == schemaKey).Distinct())
            {
                if (schemaResultRow.AuxiliaryFields.ContainsKey(field.Key) == false)
                {
                    if (documentContent.TryGetValue(field.Field, out string? documentValue) == false)
                    {
                        instance.Operation.Transaction.AddWarning(KbTransactionWarning.MethodFieldNotFound,
                            $"'{field.Key}' will be treated as null.");
                    }
                    schemaResultRow.AuxiliaryFields.Add(field.Key, documentValue);
                }
            }

            //We have to make sure that we have all of the method fields too so we can use them for calling functions.
            foreach (var field in instance.Operation.Query.GroupFields.AllDocumentFields.Where(o => o.Prefix == schemaKey).Distinct())
            {
                if (schemaResultRow.AuxiliaryFields.ContainsKey(field.Key) == false)
                {
                    if (documentContent.TryGetValue(field.Field, out string? documentValue) == false)
                    {
                        instance.Operation.Transaction.AddWarning(KbTransactionWarning.GroupFieldNotFound,
                            $"'{field.Key}' will be treated as null.");
                    }
                    schemaResultRow.AuxiliaryFields.Add(field.Key, documentValue);
                }
            }

            //We have to make sure that we have all of the condition fields too so we can filter on them.
            foreach (var field in instance.Operation.Query.Conditions.AllFields.Where(o => o.Prefix == schemaKey).Distinct())
            {
                if (schemaResultRow.AuxiliaryFields.ContainsKey(field.Key) == false)
                {
                    if (documentContent.TryGetValue(field.Field, out string? documentValue) == false)
                    {
                        instance.Operation.Transaction.AddWarning(KbTransactionWarning.ConditionFieldNotFound,
                            $"'{field.Key}' will be treated as null.");
                    }
                    schemaResultRow.AuxiliaryFields.Add(field.Key, documentValue);
                }
            }

            //We have to make sure that we have all of the sort fields too so we can filter on them.
            foreach (var field in instance.Operation.Query.SortFields.Where(o => o.Prefix == schemaKey).Distinct())
            {
                if (schemaResultRow.AuxiliaryFields.ContainsKey(field.Key) == false)
                {
                    if (documentContent.TryGetValue(field.Field, out string? documentValue) == false)
                    {
                        instance.Operation.Transaction.AddWarning(KbTransactionWarning.SortFieldNotFound,
                            $"'{field.Key}' will be treated as null.");
                    }
                    schemaResultRow.AuxiliaryFields.Add(field.Key, documentValue);
                }
            }
        }

        #endregion

        #region WHERE clause.

        /// <summary>
        /// This is where we filter the results by the WHERE clause.
        /// </summary>
        private static List<SchemaIntersectionRow> ApplyQueryGlobalConditions(
            Transaction transaction, LookupThreadInstance instance, SchemaIntersectionRowCollection inputResults)
        {
            var outputResults = new List<SchemaIntersectionRow>();
            var expression = new NCalc.Expression(instance.Operation.Query.Conditions.HighLevelExpressionTree);

            foreach (var inputResult in inputResults.Collection)
            {
                SetQueryGlobalConditionsExpressionParameters(transaction, ref expression, instance.Operation.Query.Conditions, inputResult.AuxiliaryFields);

                var ptEvaluate = instance.Operation.Transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.Evaluate);
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
        /// Sets the parameters for the WHERE clause expression evaluation from the condition field values saved from the MSQ lookup.
        /// </summary>
        private static void SetQueryGlobalConditionsExpressionParameters(Transaction transaction,
            ref NCalc.Expression expression, Conditions conditions, KbInsensitiveDictionary<string?> conditionField)
        {
            //If we have subsets, then we need to satisfy those in order to complete the equation.
            foreach (var subsetKey in conditions.Root.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetQueryGlobalConditionsExpressionParameters(transaction, ref expression, conditions, subExpression, conditionField);
            }
        }

        /// <summary>
        /// Sets the parameters for the WHERE clause expression evaluation from the condition field values saved from the MSQ lookup.
        /// </summary>
        private static void SetQueryGlobalConditionsExpressionParameters(Transaction transaction, ref NCalc.Expression expression,
            Conditions conditions, ConditionSubset conditionSubset, KbInsensitiveDictionary<string?> conditionField)
        {
            //If we have subsets, then we need to satisfy those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = conditions.SubsetByKey(subsetKey);
                SetQueryGlobalConditionsExpressionParameters(transaction, ref expression, conditions, subExpression, conditionField);
            }

            foreach (var condition in conditionSubset.Conditions)
            {
                //Get the value of the condition:
                if (conditionField.TryGetValue(condition.Left.Key, out string? value) == false)
                {
                    //Field was not found, log warning which can be returned to the user.
                    //throw new KbEngineException($"Field not found in document [{condition.Left.Key}].");
                }

                expression.Parameters[condition.ConditionKey] = condition.IsMatch(transaction, value?.ToLowerInvariant());
            }
        }

        #endregion
    }
}
