﻿using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Parsers.Query;
using NTDLS.Katzebase.Engine.Parsers.Query.Fields;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping;
using NTDLS.Katzebase.Engine.QueryProcessing.Sorting;
using NTDLS.Katzebase.Engine.Threading.PoolingParameters;
using System.Text;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Documents.DocumentPointer;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
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
            var topLevelSchemaMap = schemaMap.First().Value;

            IEnumerable<DocumentPointer>? documentPointers = null;

            //TODO: Here we should evaluate whatever conditions we can to early eliminate the top level document scans.
            //If we don't have any conditions then we just need to return all rows from the schema.
            if (topLevelSchemaMap.Optimization != null)
            {
                var limitedDocumentPointers = new List<DocumentPointer>();

                if (topLevelSchemaMap.Optimization?.IndexingConditionGroup.Count > 0)
                {
                    //We are going to create a limited document catalog from the indexes. So kill the reference and create an empty list.
                    documentPointers = new List<DocumentPointer>();

                    var indexMatchedDocuments = core.Indexes.MatchSchemaDocumentsByConditionsClause(
                        topLevelSchemaMap.PhysicalSchema, topLevelSchemaMap.Optimization, topLevelSchemaMap.Prefix);

                    if (indexMatchedDocuments != null)
                    {
                        limitedDocumentPointers.AddRange(indexMatchedDocuments.Select(o => o.Value));
                    }

                    documentPointers = limitedDocumentPointers;
                }
            }

            //If we do not have any documents, then get the whole schema.
            documentPointers ??= core.Documents.AcquireDocumentPointers(transaction, topLevelSchemaMap.PhysicalSchema, LockOperation.Read);

            var queue = core.ThreadPool.Lookup.CreateChildQueue<DocumentLookupOperation.Instance>(core.Settings.LookupOperationThreadPoolQueueDepth);

            var operation = new DocumentLookupOperation(core, transaction, schemaMap, query, gatherDocumentPointersForSchemaPrefix);

            //LogManager.Debug($"Starting document scan with {documentPointers.Count()} documents.");

            foreach (var documentPointer in documentPointers)
            {
                //We can't stop when we hit the row limit if we are sorting or grouping.
                if (query.RowLimit != 0 && query.SortFields.Any() == false
                     && query.GroupFields.Any() == false && operation.Results.Collection.Count >= query.RowLimit)
                {
                    break;
                }

                if (queue.ExceptionOccurred())
                {
                    break;
                }

                var instance = new DocumentLookupOperation.Instance(operation, documentPointer);

                var ptThreadQueue = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadQueue);
                queue.Enqueue(instance, LookupThreadWorker/*, (QueueItemState<DocumentLookupOperation.Parameter> o) =>
                {
                    //LogManager.Information($"Lookup:CompletionTime: {o.CompletionTime?.TotalMilliseconds:n0}.");
                }*/);
                ptThreadQueue?.StopAndAccumulate();
            }

            var ptThreadCompletion = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion);
            queue.WaitForCompletion();
            ptThreadCompletion?.StopAndAccumulate();

            #region Grouping.

#if DEBUG
            if (transaction.Session.IsPreLogin == false) //Debugging for non-login sessions.
            {

            }
#endif

            if (operation.Results.Collection.Count != 0 && (query.GroupFields.Count != 0 || query.SelectFields.FieldsWithAggregateFunctionCalls.Count != 0))
            {
                var resultRows = new SchemaIntersectionRowCollection();

                var FieldsWithAggregateFunctionCalls = query.SelectFields.FieldsWithAggregateFunctionCalls;

                foreach (var groupRow in operation.GroupRows)
                {
                    var resultRow = new SchemaIntersectionRow
                    {
                        Values = groupRow.Value.GroupRow,
                    };

                    foreach (var aggregateFunctionField in FieldsWithAggregateFunctionCalls)
                    {
                        var groupedValues = new KbInsensitiveDictionary<List<string>>();

                        var aggregateExpressionResult = StaticAggregateExpressionProcessor.CollapseAggregateQueryField(
                            transaction, query, groupRow.Value.GroupAggregateFunctionParameters, aggregateFunctionField);

                        resultRow.InsertValue(aggregateFunctionField.Alias, aggregateFunctionField.Ordinal, aggregateExpressionResult);
                    }


                    resultRows.Add(resultRow);
                }

                operation.Results = resultRows;
            }

            #endregion

            #region Sorting.

            //Get a list of all the fields we need to sort by.
            if (query.SortFields.Any() && operation.Results.Collection.Count != 0)
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
                var ptSorting = transaction.Instrumentation.CreateToken(PerformanceCounter.Sorting);
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

            if (query.DynamicSchemaFieldFilter != null && operation.Results.Collection.Count > 0)
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

        #region Threading: DocumentLookupThreadInstance.

        private static void LookupThreadWorker(DocumentLookupOperation.Instance instance)
        {
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

            if (instance.Operation.Query.GroupFields.Any() == false)
            {
                //We ARE NOT grouping, so collapse all field expressions as scaler expressions.
                instance.Operation.Query?.DynamicSchemaFieldSemaphore?.Wait(); //We only have to lock this is we are dynamically building the select list.
                StaticScalerExpressionProcessor.CollapseScalerRowExpressions(instance.Operation.Transaction, instance.Operation.Query.EnsureNotNull(), ref resultingRows);
                instance.Operation.Query?.DynamicSchemaFieldSemaphore?.Release();
            }
            else
            {
                var groupKey = new StringBuilder();

                foreach (var row in resultingRows.Collection)
                {
                    foreach (var groupField in instance.Operation.Query.GroupFields)
                    {
                        var collapsedGroupField = StaticScalerExpressionProcessor.CollapseScalerQueryField(instance.Operation.Transaction,
                            instance.Operation.Query.EnsureNotNull(), row.AuxiliaryFields, groupField);

                        groupKey.Append($"[{collapsedGroupField.ToLowerInvariant()}]");
                    }

                    instance.Operation.Query.EnsureNotNull();

                    lock (instance.Operation.GroupRows)
                    {
                        //What we are doing here is getting the first parameter for the aggregation functions and
                        //  collapsing that parameters expression. We then maintain a list of those aggregation functions
                        //  ExpressionKeys along with the list of group values that will need to be passed to the function
                        //  once we finally execute it.
                        if (instance.Operation.GroupRows.TryGetValue(groupKey.ToString(), out var groupRowCollection) == false)
                        {
                            groupRowCollection = new(); //Group does not yet exist, create it.
                            instance.Operation.GroupRows.Add(groupKey.ToString(), groupRowCollection);

                            //This is where we need to collapse the non aggregated field expressions. We only do this
                            //  when we CREATE the GroupRow because these values should be distinct since we are grouping.
                            //TODO: We should check these fields/expression to make sure that they are either constant or being referenced by the group clause.
                            for (int i = 0; i < row.Values.Count; i++)
                            {
                                groupRowCollection.GroupRow.Add(row.Values[i]);
                            }
                        }

                        foreach (var aggregationFunction in instance.Operation.Query.SelectFields.AggregationFunctions)
                        {
                            var aggregationArrayParam = aggregationFunction.Function.Parameters.First();

                            var collapsedAggregationParameterValue = StaticScalerExpressionProcessor.CollapseScalerExpressionFunctionParameter(
                                instance.Operation.Transaction, instance.Operation.Query.EnsureNotNull(), row.AuxiliaryFields, aggregationFunction.FunctionDependencies, aggregationArrayParam);

                            if (groupRowCollection.GroupAggregateFunctionParameters.TryGetValue(aggregationFunction.Function.ExpressionKey, out var groupAggregateFunctionParameter) == false)
                            {
                                //Create a new group detail.
                                groupAggregateFunctionParameter = new(row.AuxiliaryFields);

                                //Skip past the required AggregationArray parameter and collapse any supplemental aggregation function parameters.
                                //Supplemental parameters for aggregate functions would be something like "boolean countDistinct" for the count() function.
                                //We only do this when we create the GroupDetail because like the GroupRow, there is only one of these per group, per 
                                foreach (var supplementalParam in aggregationFunction.Function.Parameters.Skip(1))
                                {
                                    var collapsedSupplementalParamValue = StaticScalerExpressionProcessor.CollapseScalerExpressionFunctionParameter(
                                        instance.Operation.Transaction, instance.Operation.Query.EnsureNotNull(), row.AuxiliaryFields, new(), supplementalParam);

                                    groupAggregateFunctionParameter.SupplementalParameters.Add(collapsedSupplementalParamValue);
                                }

                                groupRowCollection.GroupAggregateFunctionParameters.Add(aggregationFunction.Function.ExpressionKey, groupAggregateFunctionParameter);
                            }

                            //Keep track of the values that need to be aggregated, these will be passed as the first parameter to the aggregate function.
                            groupAggregateFunctionParameter.AggregationValues.Add(collapsedAggregationParameterValue);
                        }
                    }
                }
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

        #endregion

        private static void IntersectAllSchemas(DocumentLookupOperation.Instance instance,
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
            foreach (var field in instance.Operation.Query.SelectFields.ConstantFields)
            {
                resultingRow.InsertValue(field.FieldAlias, field.Ordinal, instance.Operation.Query.Batch.GetLiteralValue(field.Value));
            }

            if (instance.Operation.SchemaMap.Count > 1)
            {
                IntersectAllSchemasRecursive(instance, 1, ref resultingRow,
                    ref resultingRows, ref threadScopedContentCache, ref joinScopedContentCache);
            }

            if (instance.Operation.Query.Conditions.AllFields.Count != 0)
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
        ///                                 This is both populated by the recursion and used as a row template for one-to-many relationships.</param>
        /// <param name="resultingRows">The buffer containing all of the rows which have been found.</param>
        /// <param name="threadScopedContentCache">Document cache for the lifetime of the entire join operation for this thread.</param>
        /// <param name="joinScopedContentCache">>Document cache used the lifetime of a single row join for this thread.</param>
        /// <exception cref="KbEngineException"></exception>
        private static void IntersectAllSchemasRecursive(DocumentLookupOperation.Instance instance,
            int skipSchemaCount, ref SchemaIntersectionRow resultingRow, ref SchemaIntersectionRowCollection resultingRows,
            ref KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> threadScopedContentCache,
            ref KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> joinScopedContentCache)
        {
            var currentSchemaKVP = instance.Operation.SchemaMap.Skip(skipSchemaCount).First();
            var currentSchemaMap = currentSchemaKVP.Value;

            var conditionHash = currentSchemaMap.Conditions.EnsureNotNull().Hash
                ?? throw new KbEngineException($"Condition hash cannot be null.");

            NCalc.Expression? expression = null;

            instance.ExpressionCache.UpgradableRead(r =>
            {
                if (r.TryGetValue(conditionHash, out expression) == false)
                {
                    expression = new NCalc.Expression(currentSchemaMap.Conditions.EnsureNotNull().Expression);

                    instance.ExpressionCache.Write(w => w.Add(conditionHash, expression));
                }
            });

            if (expression == null) throw new KbEngineException($"Expression cannot be null.");

            //Create a reference to the entire document catalog.
            IEnumerable<DocumentPointer>? documentPointers = null;

            #region Indexing to reduce the number of document pointers in "limitedDocumentPointers".

            var joinKeyValues = new KbInsensitiveDictionary<string>();

            if (currentSchemaMap.Optimization?.IndexingConditionGroup.Count > 0)
            {
                //All condition SubConditions have a selected index. Start building a list of possible document IDs.
                foreach (var subCondition in currentSchemaMap.Optimization.Conditions.NonRootSubConditions)
                {
                    //Grab the values from the schema above and save them for the index lookup of the next schema in the join.
                    foreach (var condition in subCondition.Conditions)
                    {
                        var documentContent = joinScopedContentCache[condition.Right?.Prefix ?? ""];

                        if (!documentContent.TryGetValue(condition.Right?.Value ?? "", out string? documentValue))
                        {
                            throw new KbEngineException($"Join clause field not found in document [{currentSchemaKVP.Key}].");
                        }
                        joinKeyValues[condition.Right?.Key ?? ""] = documentValue?.ToString() ?? "";
                    }
                }

                //We are going to create a limited document catalog from the indexes.

                var limitedDocumentPointers = instance.Operation.Core.Indexes.MatchSchemaDocumentsByConditionsClause(
                    currentSchemaMap.PhysicalSchema, currentSchemaMap.Optimization, currentSchemaMap.Prefix, joinKeyValues);

                documentPointers = limitedDocumentPointers.Select(o => o.Value);
            }

            documentPointers ??= instance.Operation.Core.Documents.AcquireDocumentPointers(
                    instance.Operation.Transaction, currentSchemaMap.PhysicalSchema, LockOperation.Read);

            //LogManager.Debug($"Starting join document scan with {documentPointers.Count()} documents.");

            #endregion

            int matchesFromThisSchema = 0;

            //Keep a copy of the row as it was at this level of recursion. If we find a one-to-many
            //  relationship then we will need to use this to make additional copies of the original row.
            var rowTemplate = resultingRow.Clone();

            foreach (var documentPointer in documentPointers)
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

                SetSchemaIntersectionConditionParameters(instance.Operation.Transaction,
                    ref expression, currentSchemaMap.Conditions, joinScopedContentCache);

                var ptEvaluate = instance.Operation.Transaction.Instrumentation.CreateToken(PerformanceCounter.Evaluate);
                bool evaluation = (bool)expression.Evaluate();
                ptEvaluate?.StopAndAccumulate();

                if (evaluation)
                {
                    matchesFromThisSchema++;

                    if (matchesFromThisSchema > 1)
                    {
                        //If during this level of recursion, we found more than one match then we are working with a one-to-many.
                        //This means that we will take the a copy of the original partially populated row, and create yet another
                        //  copy of it. This copy will become our new working row which we will pass recursively down for population
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
        private static void SetSchemaIntersectionConditionParameters(Transaction transaction,
            ref NCalc.Expression expression, Conditions conditions,
            KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> joinScopedContentCache)
        {
            //If we have SubConditions, then we need to satisfy those in order to complete the equation.
            foreach (var expressionKey in conditions.Root.ExpressionKeys)
            {
                var subCondition = conditions.SubConditionFromExpressionKey(expressionKey);
                SetSchemaIntersectionConditionParametersRecursive(transaction,
                    ref expression, conditions, subCondition, joinScopedContentCache);
            }
        }

        private static void SetSchemaIntersectionConditionParametersRecursive(Transaction transaction,
            ref NCalc.Expression expression, Conditions conditions, SubCondition givenSubCondition,
            KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> joinScopedContentCache)
        {
            //If we have SubConditions, then we need to satisfy those in order to complete the equation.
            foreach (var expressionKey in givenSubCondition.ExpressionKeys)
            {
                var subCondition = conditions.SubConditionFromExpressionKey(expressionKey);
                SetSchemaIntersectionConditionParametersRecursive(transaction,
                    ref expression, conditions, subCondition, joinScopedContentCache);
            }

            foreach (var condition in givenSubCondition.Conditions)
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
        private static void FillInSchemaResultDocumentValues(DocumentLookupOperation.Instance instance, string schemaKey,
            DocumentPointer documentPointer, ref SchemaIntersectionRow schemaResultRow,
            KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> threadScopedContentCache)
        {
            instance.Operation.Query?.DynamicSchemaFieldSemaphore?.Wait(); //We only have to lock this is we are dynamically building the select list.

            FillInSchemaResultDocumentValuesAtomic(instance, schemaKey, documentPointer, ref schemaResultRow, threadScopedContentCache);

            instance.Operation.Query?.DynamicSchemaFieldSemaphore?.Release();
        }

        /// <summary>
        /// Gets the values of all selected fields from document.
        /// </summary>
        private static void FillInSchemaResultDocumentValuesAtomic(DocumentLookupOperation.Instance instance, string schemaKey,
            DocumentPointer documentPointer, ref SchemaIntersectionRow schemaResultRow,
            KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> threadScopedContentCache)
        {
            var documentContent = threadScopedContentCache[$"{schemaKey}:{documentPointer.Key}"];

            if (instance.Operation.Query.DynamicSchemaFieldFilter != null) //The script is a "SELECT *". This is not optimal, but neither is select *...
            {
                if (instance.Operation.Query.DynamicSchemaFieldFilter.Count == 0 ||
                    instance.Operation.Query.DynamicSchemaFieldFilter.Contains(schemaKey))
                {
                    var fields = new List<PrefixedField>();
                    foreach (var documentValue in documentContent)
                    {
                        fields.Add(new PrefixedField(schemaKey, documentValue.Key, documentValue.Key));
                    }

                    instance.Operation.Query.SelectFields.InvalidateDocumentIdentifierFieldsCache();

                    //Add the fields to the select list where they are not already present.
                    foreach (var field in fields)
                    {
                        var currentFieldList = instance.Operation.Query.SelectFields.Select(o => o.Expression)
                            .OfType<QueryFieldDocumentIdentifier>().ToList();

                        bool isFieldAlreadyInCollection = currentFieldList.Where(o => o.Value == field.Key).ToList().Count != 0;

                        if (isFieldAlreadyInCollection == false)
                        {
                            var additionalField = new QueryField(field.Key, currentFieldList.Count, new QueryFieldDocumentIdentifier(field.Key));
                            instance.Operation.Query.SelectFields.Add(additionalField);
                        }
                    }
                }
            }

            //Keep track of which schemas we've matched on.
            schemaResultRow.SchemaKeys.Add(schemaKey);

            if (schemaKey != string.Empty)
            {
                //Grab all of the selected fields from the document for this schema.
                foreach (var field in instance.Operation.Query.SelectFields.DocumentIdentifierFields.Where(o => o.SchemaAlias == schemaKey))
                {
                    if (documentContent.TryGetValue(field.Name, out string? documentValue) == false)
                    {
                        instance.Operation.Transaction.AddWarning(KbTransactionWarning.SelectFieldNotFound,
                            $"'{field.Name}' will be treated as null.");
                    }
                    schemaResultRow.InsertValue(field.Name, field.Ordinal, documentValue);
                }
            }

            //Grab all of the selected fields from the document for the empty schema.
            foreach (var field in instance.Operation.Query.SelectFields.DocumentIdentifierFields.Where(o => o.SchemaAlias == string.Empty))
            {
                if (documentContent.TryGetValue(field.Name, out string? documentValue) == false)
                {
                    instance.Operation.Transaction.AddWarning(KbTransactionWarning.SelectFieldNotFound,
                        $"'{field.Name}' will be treated as null.");
                }
                schemaResultRow.InsertValue(field.Name, field.Ordinal, documentValue);
            }

            schemaResultRow.AuxiliaryFields.Add($"{schemaKey}.{UIDMarker}", documentPointer.Key);

            //We have to make sure that we have all of the method fields too so we can use them for calling functions.
            foreach (var field in instance.Operation.Query.SelectFields.DocumentIdentifiers.Where(o => o.Value.SchemaAlias == schemaKey).Distinct())
            {
                //TODO: AuxiliaryFields were intended to be used for satisfying functions,
                //  grouping and sorting, it seems as though the new parser just fills in everything.
                if (schemaResultRow.AuxiliaryFields.ContainsKey(field.Value.FieldName) == false)
                {
                    if (documentContent.TryGetValue(field.Value.FieldName, out string? documentValue) == false)
                    {
                        instance.Operation.Transaction.AddWarning(KbTransactionWarning.MethodFieldNotFound,
                            $"'{field.Value.FieldName}' will be treated as null.");
                    }
                    schemaResultRow.AuxiliaryFields.Add(field.Value.FieldName, documentValue);
                }
            }

            //We have to make sure that we have all of the method fields too so we can use them for calling functions.
            foreach (var field in instance.Operation.Query.GroupFields.DocumentIdentifiers.Where(o => o.Value.SchemaAlias == schemaKey).Distinct())
            {
                if (schemaResultRow.AuxiliaryFields.ContainsKey(field.Value.FieldName) == false)
                {
                    if (documentContent.TryGetValue(field.Value.FieldName, out string? documentValue) == false)
                    {
                        instance.Operation.Transaction.AddWarning(KbTransactionWarning.GroupFieldNotFound,
                            $"'{field.Value.FieldName}' will be treated as null.");
                    }
                    schemaResultRow.AuxiliaryFields.Add(field.Value.FieldName, documentValue);
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
            Transaction transaction, DocumentLookupOperation.Instance instance, SchemaIntersectionRowCollection inputResults)
        {
            var outputResults = new List<SchemaIntersectionRow>();

            NCalc.Expression? expression = null;

            var conditionHash = instance.Operation.Query.Conditions.Hash
                ?? throw new KbEngineException($"Condition hash cannot be null.");

            instance.ExpressionCache.UpgradableRead(r =>
            {
                if (r.TryGetValue(conditionHash, out expression) == false)
                {
                    expression = new NCalc.Expression(instance.Operation.Query.Conditions.Expression);
                    instance.ExpressionCache.Write(w => w.Add(conditionHash, expression));
                }
            });

            if (expression == null) throw new KbEngineException($"Expression cannot be null.");

            foreach (var inputResult in inputResults.Collection)
            {
                SetQueryGlobalConditionsExpressionParameters(transaction, ref expression, instance.Operation.Query.Conditions, inputResult.AuxiliaryFields);

                var ptEvaluate = instance.Operation.Transaction.Instrumentation.CreateToken(PerformanceCounter.Evaluate);
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
            //If we have SubConditions, then we need to satisfy those in order to complete the equation.
            foreach (var expressionKey in conditions.Root.ExpressionKeys)
            {
                var subCondition = conditions.SubConditionFromExpressionKey(expressionKey);
                SetQueryGlobalConditionsExpressionParameters(transaction, ref expression, conditions, subCondition, conditionField);
            }
        }

        /// <summary>
        /// Sets the parameters for the WHERE clause expression evaluation from the condition field values saved from the MSQ lookup.
        /// </summary>
        private static void SetQueryGlobalConditionsExpressionParameters(Transaction transaction, ref NCalc.Expression expression,
            Conditions conditions, SubCondition givenSubCondition, KbInsensitiveDictionary<string?> conditionField)
        {
            //If we have SubConditions, then we need to satisfy those in order to complete the equation.
            foreach (var expressionKey in givenSubCondition.ExpressionKeys)
            {
                var subCondition = conditions.SubConditionFromExpressionKey(expressionKey);
                SetQueryGlobalConditionsExpressionParameters(transaction, ref expression, conditions, subCondition, conditionField);
            }

            foreach (var condition in givenSubCondition.Conditions)
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