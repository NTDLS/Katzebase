using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.QueryProcessing.Expressions;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping;
using NTDLS.Katzebase.Engine.QueryProcessing.Sorting;
using NTDLS.Katzebase.Parsers.Query.Conditions;
using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.PersistentTypes.Document;
using System.Text;
using static NTDLS.Katzebase.Api.KbConstants;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    /// <summary>
    /// Used to obtain rows by a prepared query, joining schemas and applying where clause.
    /// </summary>
    internal static class StaticSchemaIntersectionProcessor
    {
        /// <summary>
        /// Generates a set of rows and field names using a prepared query.
        /// </summary>
        /// <param name="gatherDocumentPointersForSchemaAliases">When not null, the process will focus on
        /// obtaining a list of DocumentPointers instead of fields/rows. This is used for UPDATES and DELETES.</param>
        /// <returns></returns>
        internal static DocumentLookupRowCollection GetDocumentsByConditions(EngineCore core, Transaction transaction,
            QuerySchemaMap schemaMap, PreparedQuery query)
        {
            var intersectedRowCollection = GatherIntersectedRows(core, transaction, schemaMap, query);

            transaction.EnsureActive();

            #region Hydrate dynamic field list (select *).

            if (query.DynamicSchemaFieldFilter != null)
            {
                var distinctFields = new HashSet<string>();

                //Get the distinct values "field names" all schemas.
                foreach (var intersectedRow in intersectedRowCollection)
                {
                    transaction.EnsureActive();

                    foreach (var schema in intersectedRow.SchemaElements.Where(o =>
                        query.DynamicSchemaFieldFilter.Count == 0 //Get fields from all schemas
                        || query.DynamicSchemaFieldFilter.Contains(o.Key, StringComparer.InvariantCultureIgnoreCase))) //Get fields from specific schemas.
                    {
                        foreach (var field in schema.Value)
                        {
                            distinctFields.Add($"{schema.Key}.{field.Key}".TrimStart('.'));
                        }
                    }
                }

                foreach (var field in distinctFields.OrderBy(o => o))
                {
                    query.SelectFields.Add(new QueryField(field, query.SelectFields.Count, new QueryFieldDocumentIdentifier(query.ScriptLine, field)));
                }
            }

            #endregion

            var materializedRowCollection = MaterializeRowValues(core, transaction, query, intersectedRowCollection);

            transaction.EnsureActive();

            if (query.PreTopDistinct)
            {
                materializedRowCollection.RemoveDuplicateRows();
            }

            #region Sorting (perform the final sort).

            if (query.OrderBy.Any() && materializedRowCollection.Rows.Count != 0)
            {
                var sortingColumns = new List<(string fieldAlias, KbSortDirection sortDirection)>();
                foreach (var sortField in query.OrderBy)
                {
                    sortingColumns.Add(new(sortField.Alias, sortField.SortDirection));
                }

                //Sort the results:
                var ptSorting = transaction.Instrumentation.CreateToken(PerformanceCounter.Sorting);
                materializedRowCollection.Rows.Sort((x, y) => MaterializedRowComparer.Compare(sortingColumns, x, y));
                ptSorting?.StopAndAccumulate();
            }

            #endregion

            transaction.EnsureActive();

            #region Enforce row limits.

            if (query.RowLimit > 0 && materializedRowCollection.Rows.Count > query.RowLimit)
            {
                if (query.RowOffset == 0)
                {
                    materializedRowCollection.Rows.RemoveRange(query.RowLimit, materializedRowCollection.Rows.Count - query.RowLimit);
                }
                else
                {
                    if (query.RowOffset > materializedRowCollection.Rows.Count)
                    {
                        materializedRowCollection.Rows.Clear();
                    }
                    else
                    {
                        materializedRowCollection.Rows.RemoveRange(0, query.RowOffset);

                        if (materializedRowCollection.Rows.Count > query.RowLimit)
                        {
                            materializedRowCollection.Rows.RemoveRange(query.RowLimit, materializedRowCollection.Rows.Count - query.RowLimit);
                        }
                    }
                }
            }
            else if (query.RowOffset > 0)
            {
                if (query.RowOffset > materializedRowCollection.Rows.Count)
                {
                    materializedRowCollection.Rows.Clear();
                }
                else
                {
                    materializedRowCollection.Rows.RemoveRange(0, query.RowOffset);
                }
            }

            #endregion

            if (query.PostTopDistinct)
            {
                materializedRowCollection.RemoveDuplicateRows();
            }

            transaction.EnsureActive();

            return new DocumentLookupRowCollection(materializedRowCollection.Rows, materializedRowCollection.DocumentIdentifiers);
        }

        /// <summary>
        /// First obtains results from the primary schema using indexing and the WHERE clause, then combines all subsequent
        /// joined schemas (also using indexing). Expands rowset for one-to-many, many-to-many and many-to-one joins.
        /// </summary>
        public static SchemaIntersectionRowCollection GatherIntersectedRows(EngineCore core, Transaction transaction,
            QuerySchemaMap schemaMappings, PreparedQuery query, List<string>? gatherDocumentPointersForSchemaAliases = null)
        {
            var resultingRowCollection = GatherPrimarySchemaRows(core, transaction, schemaMappings, query, gatherDocumentPointersForSchemaAliases);

            var childPool = core.ThreadPool.Intersection.CreateChildPool<SchemaIntersectionRow>(core.Settings.IntersectionChildThreadPoolQueueDepth);

            bool rowLimitExceeded = false;

            //Skip the primary schema because those rows were collected by GatherPrimarySchemaRows().
            //Loop through all schemas
            foreach (var schemaMap in schemaMappings.Skip(1))
            {
                var schemaIntersectionRowCollection = new SchemaIntersectionRowCollection();

                //Loop though all rows and gather the document elements from the current schema for all JOIN condition matches.
                foreach (var templateRow in resultingRowCollection)
                {
                    transaction.EnsureActive();

                    var ptThreadQueue = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadQueue);
                    childPool.Enqueue(templateRow.Clone(), (threadTemplateRowClone) =>
                    {
                        #region Thread.

                        transaction.EnsureActive();

                        IEnumerable<DocumentPointer>? documentPointers = null;

                        #region Index optimization.

                        if (schemaMap.Value.Optimization?.IndexingConditionGroup.Count > 0)
                        {
                            var keyValues = new KbInsensitiveDictionary<string?>();

                            //Grab the values from the right-hand-schema join clause and create a lookup of the values.
                            var rightHandDocumentIdentifiers = schemaMap.Value.Optimization.Conditions.FlattenToRightDocumentIdentifiers();
                            foreach (var documentIdentifier in rightHandDocumentIdentifiers)
                            {
                                if (!threadTemplateRowClone.SchemaElements.TryGetValue(documentIdentifier.SchemaAlias, out var schemaElements))
                                {
                                    throw new KbEngineException($"Schema not found in query: [{documentIdentifier.SchemaAlias}].");
                                }

                                if (!schemaElements.TryGetValue(documentIdentifier.FieldName, out var schemaElement))
                                {
                                    transaction.AddWarning(KbTransactionWarning.FieldNotFound, documentIdentifier.Value.EnsureNotNull());
                                }

                                keyValues[documentIdentifier.FieldName] = schemaElement;
                            }

                            //We are going to create a limited document catalog using the indexes.
                            var indexMatchedDocuments = core.Indexes.MatchSchemaDocumentsByConditionsClause(
                                schemaMap.Value.PhysicalSchema, schemaMap.Value.Optimization, query, schemaMap.Value.Prefix, keyValues);

                            documentPointers = indexMatchedDocuments.Select(o => o.Value);
                        }

                        #endregion

                        //If we do not have any documents then indexing was not performed, then get the whole schema.
                        documentPointers ??= core.Documents.AcquireDocumentPointers(transaction, schemaMap.Value.PhysicalSchema, LockOperation.Read);

                        int schemaMatchCount = 0;

                        foreach (var documentPointer in documentPointers)
                        {
                            var physicalDocument = core.Documents.AcquireDocument(transaction, schemaMap.Value.PhysicalSchema, documentPointer, LockOperation.Read);

                            threadTemplateRowClone.SchemaElements[schemaMap.Value.Prefix.ToLowerInvariant()] = physicalDocument.Elements;

                            if (IsJoinExpressionMatch(transaction, query, schemaMap.Value.Conditions, threadTemplateRowClone))
                            {
                                schemaMatchCount++;

                                var newRow = threadTemplateRowClone.Clone();
                                newRow.MatchedSchemas.Add(schemaMap.Key);

                                if (gatherDocumentPointersForSchemaAliases?.Contains(schemaMap.Value.Prefix, StringComparer.InvariantCultureIgnoreCase) == true)
                                {
                                    //Keep track of document pointers for this schema if we are to do so as denoted by gatherDocumentPointersForSchemaAliases.
                                    newRow.DocumentPointers.Add(schemaMap.Value.Prefix.ToLowerInvariant(), documentPointer);
                                }

                                //Found a document that matched the join clause, add row to the results collection.
                                lock (schemaIntersectionRowCollection)
                                {
                                    schemaIntersectionRowCollection.Add(newRow);

                                    //Test to see if we've hit a row limit.
                                    //Not that we cannot limit early when we have an ORDER BY because limiting is done after sorting the results.
                                    if (query.RowOffset == 0 && query.RowLimit > 0 && schemaIntersectionRowCollection.Count >= query.RowLimit && query.OrderBy.Count == 0)
                                    {
                                        rowLimitExceeded = true;
                                        break;
                                    }
                                }
                            }
                        }

                        //If this is a left-outer join and we didn't find a match, then add a dummy row for this join.
                        if (schemaMatchCount == 0 && schemaMap.Value.SchemaUsageType == QuerySchema.QuerySchemaUsageType.OuterJoin)
                        {
                            var newRow = threadTemplateRowClone.Clone();
                            newRow.SchemaElements[schemaMap.Value.Prefix.ToLowerInvariant()] = new KbInsensitiveDictionary<string?>();
                            newRow.MatchedSchemas.Add(schemaMap.Key);

                            lock (schemaIntersectionRowCollection)
                            {
                                schemaIntersectionRowCollection.Add(newRow);
                            }
                        }

                        if (rowLimitExceeded)
                        {
                            return; //Break out of thread.
                        }

                        #endregion
                    });
                    ptThreadQueue?.StopAndAccumulate();
                }

                var ptThreadCompletion = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion);
                childPool.WaitForCompletion();
                ptThreadCompletion?.StopAndAccumulate();

                resultingRowCollection = schemaIntersectionRowCollection.Clone();
            }

            //Get the aliases for all schemas that a row is required to be comprised of.
            var requiredSchemas = schemaMappings.Where(o =>
                o.Value.SchemaUsageType == QuerySchema.QuerySchemaUsageType.Primary
                || o.Value.SchemaUsageType == QuerySchema.QuerySchemaUsageType.InnerJoin).Select(o => o.Key).ToList();

            //Remove any rows where the required schemas were not matched. I have not proven that this is necessary,
            //  but it is included to ensure that all resulting rows have been matched to all schemas in the case that
            //  we abandon the matching process partially though joining due to early exit while respecting "TOP n" row limiter.
            resultingRowCollection.RemoveAll(o => o.MatchedSchemas.All(m => requiredSchemas.Contains(m) == false));

            //Now that we have finished joining all schemas, we can now apply the WHERE clause.
            var primarySchema = schemaMappings.First();

            var matchChildPool = core.ThreadPool.Intersection.CreateChildPool<SchemaIntersectionRow>(core.Settings.IntersectionChildThreadPoolQueueDepth);

            foreach (var resultingRow in resultingRowCollection)
            {
                var ptThreadQueue = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadQueue);
                matchChildPool.Enqueue(resultingRow, (threadResultingRow) =>
                {
                    #region Thread.

                    var schemaElements = threadResultingRow.SchemaElements.Flatten();
                    threadResultingRow.MatchedByWhereClause = IsWhereClauseMatch(transaction, query, primarySchema.Value.Conditions, schemaElements);

                    #endregion
                });
                ptThreadQueue?.StopAndAccumulate();
            }

            var ptThreadCompletion_Removal = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion);
            matchChildPool.WaitForCompletion();
            ptThreadCompletion_Removal?.StopAndAccumulate();

            //Remove all rows that were not matched by the where clause.
            resultingRowCollection.RemoveAll(o => !o.MatchedByWhereClause);

            #region Internal IsJoinExpressionMatch()

            /// <summary>
            /// Collapses all left-and-right condition values, compares them, and fills in the expression variables with the comparison result.
            /// </summary>
            static bool IsJoinExpressionMatch(Transaction transaction,
            PreparedQuery query, ConditionCollection? givenConditions, SchemaIntersectionRow schemaIntersectionRow)
            {
                if (givenConditions == null)
                {
                    //There are no conditions, so this is a match.
                    return true;
                }

                var matchExpression = new NCalc.Expression(givenConditions.MathematicalExpression);

                SetJoinExpressionParametersRecursive(givenConditions.Collection);

                var ptEvaluate = transaction.Instrumentation.CreateToken(PerformanceCounter.Evaluate);
                bool evaluation = (bool)matchExpression.Evaluate();
                ptEvaluate?.StopAndAccumulate();

                return evaluation;

                void SetJoinExpressionParametersRecursive(List<ICondition> conditions)
                {
                    foreach (var condition in conditions)
                    {
                        if (condition is ConditionGroup group)
                        {
                            SetJoinExpressionParametersRecursive(group.Collection);
                        }
                        else if (condition is ConditionEntry entry)
                        {
                            var leftDocumentContent = schemaIntersectionRow.SchemaElements[entry.Left.SchemaAlias];
                            var collapsedLeft = entry.Left.CollapseScalarQueryField(transaction,
                                query, givenConditions.FieldCollection, leftDocumentContent)?.ToLowerInvariant();

                            var rightDocumentContent = schemaIntersectionRow.SchemaElements[entry.Right.SchemaAlias];
                            var collapsedRight = entry.Right.CollapseScalarQueryField(transaction,
                                query, givenConditions.FieldCollection, rightDocumentContent)?.ToLowerInvariant();

                            matchExpression.Parameters[entry.ExpressionVariable] = entry.IsMatch(collapsedLeft, collapsedRight);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }

            #endregion

            #region Internal IsWhereClauseMatch().

            static bool IsWhereClauseMatch(Transaction transaction, PreparedQuery query,
                ConditionCollection? givenConditions, KbInsensitiveDictionary<string?> documentElements)
            {
                if (givenConditions == null)
                {
                    //There are no conditions, so this is a match.
                    return true;
                }

                var matchExpression = new NCalc.Expression(givenConditions.MathematicalExpression);

                SetExpressionParametersRecursive(givenConditions.Collection);

                var ptEvaluate = transaction.Instrumentation.CreateToken(PerformanceCounter.Evaluate);
                bool evaluation = (bool)matchExpression.Evaluate();
                ptEvaluate?.StopAndAccumulate();

                return evaluation;

                void SetExpressionParametersRecursive(List<ICondition> conditions)
                {
                    foreach (var condition in conditions)
                    {
                        if (condition is ConditionGroup group)
                        {
                            SetExpressionParametersRecursive(group.Collection);
                        }
                        else if (condition is ConditionEntry entry)
                        {
                            var collapsedLeft = entry.Left.CollapseScalarQueryField(transaction, query, givenConditions.FieldCollection, documentElements)?.ToLowerInvariant();
                            var collapsedRight = entry.Right.CollapseScalarQueryField(transaction, query, givenConditions.FieldCollection, documentElements)?.ToLowerInvariant();

                            matchExpression.Parameters[entry.ExpressionVariable] = entry.IsMatch(collapsedLeft, collapsedRight);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }

            #endregion

            return resultingRowCollection;
        }

        /// <summary>
        /// Gets a collection of WHERE clause qualified rows, in parallel, from the first schema in th query.
        /// </summary>
        private static SchemaIntersectionRowCollection GatherPrimarySchemaRows(EngineCore core, Transaction transaction,
            QuerySchemaMap schemaMappings, PreparedQuery query, List<string>? gatherDocumentPointersForSchemaAliases)
        {
            var primarySchema = schemaMappings.First();
            IEnumerable<DocumentPointer>? documentPointers = null;

            if (primarySchema.Value.Optimization?.IndexingConditionGroup.Count > 0)
            {
                //We are going to create a limited document catalog using the indexes.
                var indexMatchedDocuments = core.Indexes.MatchSchemaDocumentsByConditionsClause(
                    primarySchema.Value.PhysicalSchema, primarySchema.Value.Optimization, query, primarySchema.Value.Prefix);

                documentPointers = indexMatchedDocuments.Select(o => o.Value);
            }

            //If we do not have any documents then indexing was not performed, then get the whole schema.
            documentPointers ??= core.Documents.AcquireDocumentPointers(transaction, primarySchema.Value.PhysicalSchema, LockOperation.Read);

            var schemaIntersectionRowCollection = new SchemaIntersectionRowCollection();

            var childPool = core.ThreadPool.Lookup.CreateChildPool<DocumentPointer>(core.Settings.LookupChildThreadPoolQueueDepth);

            bool rowLimitExceeded = false;

            foreach (var documentPointer in documentPointers)
            {
                transaction.EnsureActive();

                var ptThreadQueue = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadQueue);
                childPool.Enqueue(documentPointer, (threadDocumentPointer) =>
                {
                    #region Thread.

                    transaction.EnsureActive();

                    var physicalDocument = core.Documents.AcquireDocument(transaction, primarySchema.Value.PhysicalSchema, threadDocumentPointer, LockOperation.Read);

                    //Had to remove this match because the where clause can contain conditions comprised of values from joins.
                    //TODO: We use condition groups to determine if we can do early elimination of primary schema results.
                    //if (IsWhereClauseMatch(transaction, query, primarySchema.Value.Conditions, physicalDocument.Elements))
                    //{
                    var schemaIntersectionRow = new SchemaIntersectionRow();
                    schemaIntersectionRow.MatchedSchemas.Add(primarySchema.Key);

                    schemaIntersectionRow.SchemaElements.Add(primarySchema.Value.Prefix.ToLowerInvariant(), physicalDocument.Elements);

                    if (gatherDocumentPointersForSchemaAliases?.Contains(primarySchema.Value.Prefix, StringComparer.InvariantCultureIgnoreCase) == true)
                    {
                        //Keep track of document pointers for this schema if we are to do so as denoted by gatherDocumentPointersForSchemaAliases.
                        schemaIntersectionRow.DocumentPointers.Add(primarySchema.Value.Prefix.ToLowerInvariant(), threadDocumentPointer);
                    }

                    //Found a document, add row to the results collection.
                    lock (schemaIntersectionRowCollection)
                    {
                        schemaIntersectionRowCollection.Add(schemaIntersectionRow);

                        //Test to see if we've hit a row limit.
                        //Not that we cannot limit early when we have an ORDER BY because limiting is done after sorting the results.
                        if (query.RowOffset == 0 && query.RowLimit > 0 && schemaIntersectionRowCollection.Count >= query.RowLimit && query.OrderBy.Count == 0)
                        {
                            rowLimitExceeded = true;
                            return; //Break out of thread.
                        }
                    }
                    //}

                    #endregion
                });
                ptThreadQueue?.StopAndAccumulate();

                if (rowLimitExceeded)
                {
                    break;
                }
            }

            var ptThreadCompletion = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion);
            childPool.WaitForCompletion();
            ptThreadCompletion?.StopAndAccumulate();

            return schemaIntersectionRowCollection;
        }

        /// <summary>
        /// Takes a set of data from GatherIntersectedRows() and collapses all expressions on all rows, performs grouping,
        ///     scalar and aggregate function execution, and builds a expression-collapsed list of all values required for sorting.
        /// </summary>
        private static MaterializedRowCollection MaterializeRowValues(EngineCore core, Transaction transaction,
            PreparedQuery query, SchemaIntersectionRowCollection intersectedRowCollection)
        {
            var materializedRowCollection = new MaterializedRowCollection();

            if (query.GroupBy.Any() == false && query.SelectFields.FieldsWithAggregateFunctionCalls.Count == 0)
            {
                #region No Grouping.

                var childPool = core.ThreadPool.Materialization.CreateChildPool<SchemaIntersectionRow>(core.Settings.MaterializationChildThreadPoolQueueDepth);

                bool rowLimitExceeded = false;

                foreach (var row in intersectedRowCollection)
                {
                    transaction.EnsureActive();

                    var ptThreadQueue = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadQueue);
                    childPool.Enqueue(row, (threadRow) =>
                    {
                        #region Thread.

                        transaction.EnsureActive();

                        var materializedRow = new MaterializedRow();
                        var flattenedSchemaElements = threadRow.SchemaElements.Flatten();

                        foreach (var field in query.SelectFields)
                        {
                            var value = MaterializeRowField(transaction, query, threadRow, flattenedSchemaElements, field, FieldCollapseType.ScalerSelect);
                            materializedRow.Values.InsertWithPadding(field.Alias, field.Ordinal, value);
                        }

                        #region Collapse all values needed for sorting.

                        foreach (var field in query.OrderBy)
                        {
                            var value = MaterializeRowField(transaction, query, threadRow, flattenedSchemaElements, field, FieldCollapseType.ScalerOrderBy);
                            materializedRow.OrderByValues.Add(field.Alias, value);
                        }

                        #endregion

                        lock (childPool)
                        {
                            materializedRowCollection.Rows.Add(materializedRow);

                            //Test to see if we've hit a row limit.
                            //Not that we cannot limit early when we have an ORDER BY because limiting is done after sorting the results.
                            if (query.RowOffset == 0 && query.RowLimit > 0 && materializedRowCollection.Rows.Count >= query.RowLimit && query.OrderBy.Count == 0)
                            {
                                rowLimitExceeded = true;
                                return; //Break out of thread.
                            }
                        }

                        #endregion
                    });
                    ptThreadQueue?.StopAndAccumulate();

                    if (rowLimitExceeded)
                    {
                        break;
                    }
                }

                var ptThreadCompletion = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion);
                childPool.WaitForCompletion();
                ptThreadCompletion?.StopAndAccumulate();

                #endregion
            }
            else
            {
                #region Grouping.

                /// <summary>
                /// Contains the list of field values for the grouping fields, and the need-to-be aggregated values for fields
                /// that are needed to collapse aggregation functions. The key is the concatenated values from the grouping fields.
                /// </summary>
                var groupRows = new Dictionary<string, GroupRow>();

                foreach (var row in intersectedRowCollection)
                {
                    transaction.EnsureActive();

                    var flattenedSchemaElements = row.SchemaElements.Flatten();

                    var groupKey = new StringBuilder();

                    foreach (var groupField in query.GroupBy)
                    {
                        var collapsedGroupField = groupField.Expression.CollapseScalarQueryField(
                            transaction, query, query.GroupBy, flattenedSchemaElements);

                        groupKey.Append($"[{collapsedGroupField?.ToLowerInvariant()}]");
                    }

                    if (groupRows.TryGetValue(groupKey.ToString(), out var groupRow) == false)
                    {
                        #region Creation of group row.

                        groupRow = new(); //Group does not yet exist, create it.
                        groupRows.Add(groupKey.ToString(), groupRow);

                        //This is where we need to collapse the non aggregated field expressions. We only do this
                        //  when we CREATE the GroupRow because these values should be distinct since we are grouping.
                        //TODO: We should check these fields/expression to make sure that they are either constant or being referenced by the group clause.
                        foreach (var field in query.SelectFields)
                        {
                            var value = MaterializeRowField(transaction, query, row, flattenedSchemaElements, field, FieldCollapseType.AggregateSelect);
                            groupRow.Values.InsertWithPadding(field.Alias, field.Ordinal, value);
                        }

                        #endregion

                        #region Collapse all values needed for sorting.

                        foreach (var field in query.OrderBy)
                        {
                            var value = MaterializeRowField(transaction, query, row, flattenedSchemaElements, field, FieldCollapseType.AggregateOrderBy);
                            groupRow.OrderByValues.Add(field.Alias, value);
                        }

                        #endregion
                    }

                    #region Collect and collapse expressions that are to be used for aggregation function execution.

                    #region Select Fields.

                    foreach (var aggregationFunction in query.SelectFields.AggregationFunctions)
                    {
                        //The first parameter for an aggregation function is the "values array", get it so we can add values to it.
                        var aggregationArrayParam = aggregationFunction.Function.Parameters.First();

                        //All aggregation parameters are collapsed here at query processing time.
                        var collapsedAggregationParameterValue = aggregationArrayParam.CollapseScalarExpression(transaction,
                            query.EnsureNotNull(), query.SelectFields, flattenedSchemaElements, aggregationFunction.FunctionDependencies);

                        //If the aggregation function parameters do not yey exist for this function then create them.
                        if (groupRow.SelectAggregateFunctionParameters.TryGetValue(
                            aggregationFunction.Function.ExpressionKey, out var groupAggregateFunctionParameter) == false)
                        {
                            groupAggregateFunctionParameter = new();

                            //Skip past the required AggregationArray parameter and collapse any supplemental aggregation function parameters.
                            //Supplemental parameters for aggregate functions would be something like "boolean countDistinct" for the count() function.
                            //We only do this when we CREATE the group parameters because like the GroupRow, there is only one of these per group, per 
                            foreach (var supplementalParam in aggregationFunction.Function.Parameters.Skip(1))
                            {
                                var collapsedSupplementalParamValue = supplementalParam.CollapseScalarExpression(
                                    transaction, query, query.SelectFields, flattenedSchemaElements, new());

                                groupAggregateFunctionParameter.SupplementalParameters.Add(collapsedSupplementalParamValue);
                            }

                            //Add this parameter collection to the lookup so we can add additional values to it with subsequent rows.
                            groupRow.SelectAggregateFunctionParameters.Add(aggregationFunction.Function.ExpressionKey, groupAggregateFunctionParameter);
                        }

                        //Keep track of the values that need to be aggregated, these will be passed as the first parameter to the aggregate function.
                        if (collapsedAggregationParameterValue != null)
                        {
                            //Add the collapsed expression to the aggregation values array.
                            groupAggregateFunctionParameter.AggregationValues.Add(collapsedAggregationParameterValue);
                        }
                        else
                        {
                            transaction.AddWarning(KbTransactionWarning.NullValuePropagation);
                        }
                    }

                    #endregion

                    #region Order By.

                    foreach (var aggregationFunction in query.OrderBy.AggregationFunctions)
                    {
                        //The first parameter for an aggregation function is the "values array", get it so we can add values to it.
                        var aggregationArrayParam = aggregationFunction.Function.Parameters.First();

                        //All aggregation parameters are collapsed here at query processing time.
                        var collapsedAggregationParameterValue = aggregationArrayParam.CollapseScalarExpression(transaction,
                            query.EnsureNotNull(), query.OrderBy, flattenedSchemaElements, aggregationFunction.FunctionDependencies);

                        //If the aggregation function parameters do not yey exist for this function then create them.
                        if (groupRow.SortAggregateFunctionParameters.TryGetValue(
                            aggregationFunction.Function.ExpressionKey, out var groupAggregateFunctionParameter) == false)
                        {
                            groupAggregateFunctionParameter = new();

                            //Skip past the required AggregationArray parameter and collapse any supplemental aggregation function parameters.
                            //Supplemental parameters for aggregate functions would be something like "boolean countDistinct" for the count() function.
                            //We only do this when we CREATE the group parameters because like the GroupRow, there is only one of these per group, per 
                            foreach (var supplementalParam in aggregationFunction.Function.Parameters.Skip(1))
                            {
                                var collapsedSupplementalParamValue = supplementalParam.CollapseScalarExpression(
                                    transaction, query, query.OrderBy, flattenedSchemaElements, new());

                                groupAggregateFunctionParameter.SupplementalParameters.Add(collapsedSupplementalParamValue);
                            }

                            //Add this parameter collection to the lookup so we can add additional values to it with subsequent rows.
                            groupRow.SortAggregateFunctionParameters.Add(aggregationFunction.Function.ExpressionKey, groupAggregateFunctionParameter);
                        }

                        //Keep track of the values that need to be aggregated, these will be passed as the first parameter to the aggregate function.
                        if (collapsedAggregationParameterValue != null)
                        {
                            //Add the collapsed expression to the aggregation values array.
                            groupAggregateFunctionParameter.AggregationValues.Add(collapsedAggregationParameterValue);
                        }
                        else
                        {
                            transaction.AddWarning(KbTransactionWarning.NullValuePropagation);
                        }
                    }

                    #endregion

                    #endregion
                }

                //The operation.GroupRows contains the resulting row template, with all fields in the correct position,
                //  all that is left to do is execute the aggregation functions and fill in the appropriate fields.
                foreach (var groupRow in groupRows)
                {
                    transaction.EnsureActive();

                    var materializedRow = new MaterializedRow(groupRow.Value.Values, groupRow.Value.OrderByValues);

                    //Execute aggregate functions for SELECT fields:
                    foreach (var selectAggregateFunctionField in query.SelectFields.FieldsWithAggregateFunctionCalls)
                    {
                        var aggregateExpressionResult = selectAggregateFunctionField.CollapseAggregateQueryField(transaction, query, groupRow.Value.SelectAggregateFunctionParameters);
                        //Insert the aggregation result into the proper position in the values list.
                        materializedRow.Values.InsertWithPadding(selectAggregateFunctionField.Alias, selectAggregateFunctionField.Ordinal, aggregateExpressionResult);
                    }

                    //Execute aggregate functions for ORDER BY fields:
                    foreach (var orderByAggregateFunctionField in query.OrderBy.FieldsWithAggregateFunctionCalls)
                    {
                        var aggregateExpressionResult = orderByAggregateFunctionField.CollapseAggregateQueryField(transaction, query, groupRow.Value.SortAggregateFunctionParameters);

                        //Save the aggregation result in the ORDER BY collection. 
                        materializedRow.OrderByValues[orderByAggregateFunctionField.Alias] = aggregateExpressionResult;
                    }

                    materializedRowCollection.Rows.Add(materializedRow);

                    //Test to see if we've hit a row limit.
                    //Not that we cannot limit early when we have an ORDER BY because limiting is done after sorting the results.
                    if (query.RowLimit > 0 && materializedRowCollection.Rows.Count >= query.RowLimit && query.OrderBy.Count == 0)
                    {
                        break;
                    }
                }

                #endregion
            }

            return materializedRowCollection;
        }

        private static string? MaterializeRowField(Transaction transaction, PreparedQuery query, SchemaIntersectionRow row,
            KbInsensitiveDictionary<string?> flattenedSchemaElements, QueryField field, FieldCollapseType fieldCollapseType)
        {
            if (field.Expression is QueryFieldDocumentIdentifier fieldDocumentIdentifier)
            {
                if (!row.SchemaElements.TryGetValue(fieldDocumentIdentifier.SchemaAlias, out var schemaElements))
                {
                    throw new KbEngineException($"Schema not found in query: [{fieldDocumentIdentifier.SchemaAlias}].");
                }

                if (!schemaElements.TryGetValue(fieldDocumentIdentifier.FieldName, out var schemaElement))
                {
                    transaction.AddWarning(KbTransactionWarning.FieldNotFound, fieldDocumentIdentifier.Value.EnsureNotNull());
                }

                if (fieldCollapseType == FieldCollapseType.ScalerOrderBy || fieldCollapseType == FieldCollapseType.AggregateOrderBy)
                {
                    if (double.TryParse(schemaElement, out _))
                    {
                        //Pad numeric value for proper numeric ordering.
                        schemaElement = schemaElement.PadLeft(25 + schemaElement.Length, '0');
                    }
                }

                return schemaElement;
            }
            else if (field.Expression is IQueryFieldExpression fieldExpression)
            {
                if (fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionAggregate>().Any() == false)
                {
                    var collapsedValue = fieldExpression.CollapseScalarQueryField(transaction, query, query.SelectFields, flattenedSchemaElements);

                    if (fieldCollapseType == FieldCollapseType.ScalerOrderBy || fieldCollapseType == FieldCollapseType.AggregateOrderBy)
                    {
                        if (double.TryParse(collapsedValue, out _))
                        {
                            //Pad numeric value for proper numeric ordering.
                            collapsedValue = collapsedValue.PadLeft(25 + collapsedValue.Length, '0');
                        }
                    }

                    return collapsedValue;
                }
                else
                {
                    if (fieldCollapseType != FieldCollapseType.AggregateOrderBy && fieldCollapseType != FieldCollapseType.AggregateSelect)
                    {
                        throw new KbEngineException("Aggregate function found during scalar materialization.");
                    }
                    else
                    {
                        return null; //These values will be filled in during aggregation execution.
                    }
                }
            }
            else if (field.Expression is QueryFieldConstantNumeric constantNumeric)
            {
                var numericValue = query.Batch.Variables.Resolve(constantNumeric.Value.EnsureNotNull());

                if (fieldCollapseType == FieldCollapseType.ScalerOrderBy || fieldCollapseType == FieldCollapseType.AggregateOrderBy)
                {
                    //Pad numeric value for proper numeric ordering.
                    numericValue = numericValue?.PadLeft(25 + numericValue.Length, '0');
                }

                return numericValue;
            }
            else if (field.Expression is QueryFieldConstantString constantString)
            {
                var stringValue = query.Batch.Variables.Resolve(constantString.Value.EnsureNotNull());
                return stringValue;
            }

            throw new KbNotImplementedException($"Type was not handled: [{field.Expression.GetType()}].");
        }

    }
}
