using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping;
using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.Fields.Expressions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions.Helpers;
using NTDLS.Katzebase.PersistentTypes.Document;
using System.Text;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal static class StaticSchemaIntersectionMethods
    {
        /// <summary>
        /// Build a generic key/value dataset which is the combined field-set from each inner joined document.
        /// </summary>
        /// <param name="gatherDocumentsIdsForSchemaPrefixes">When not null, the process will focus on
        /// obtaining a list of DocumentPointers instead of key/values. This is used for UPDATES and DELETES.</param>
        /// <returns></returns>
        internal static DocumentLookupResults GetDocumentsByConditions(EngineCore core, Transaction transaction,
            QuerySchemaMap schemaMap, PreparedQuery query, string[]? gatherDocumentsIdsForSchemaPrefixes = null)
        {
            var intersectedRowCollection = GatherIntersectedRows(core, transaction, schemaMap, query, gatherDocumentsIdsForSchemaPrefixes);

            var materializedRows = MaterializeRowValues(core, transaction, schemaMap, query, intersectedRowCollection);

            //This is just for debugging.
            var lookupResults = new DocumentLookupResults(materializedRows.Values, materializedRows.DocumentIdentifiers);

            //TODO: Sorting

            //TODO: Limiting

            //TODO: Offset

            return lookupResults;
        }

        private static MaterializedRowValues MaterializeRowValues(EngineCore core, Transaction transaction,
            QuerySchemaMap schemaMap, PreparedQuery query, SchemaIntersectionRowCollection intersectedRowCollection)
        {
            var materializedRowValues = new MaterializedRowValues();

            if (query.GroupBy.Any() == false && query.SelectFields.FieldsWithAggregateFunctionCalls.Count == 0)
            {
                #region No Grouping.

                var childPool = core.ThreadPool.Materialization.CreateChildQueue(core.Settings.MaterializationChildThreadPoolQueueDepth);

                foreach (var row in intersectedRowCollection)
                {
                    childPool.Enqueue(() =>
                    {
                        var rowFieldValues = new List<string?>();
                        var flattenedSchemaElements = row.SchemaElements.Flatten();

                        foreach (var field in query.SelectFields)
                        {
                            if (field.Expression is QueryFieldDocumentIdentifier fieldDocumentIdentifier)
                            {
                                var fieldValue = row.SchemaElements[fieldDocumentIdentifier.SchemaAlias][fieldDocumentIdentifier.FieldName];
                                rowFieldValues.InsertWithPadding(field.Alias, field.Ordinal, fieldValue);
                            }
                            else if (field.Expression is IQueryFieldExpression fieldExpression)
                            {
                                if (fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionAggregate>().Any() == false)
                                {
                                    var collapsedValue = StaticScalarExpressionProcessor.CollapseScalarQueryField(
                                        fieldExpression, transaction, query, query.SelectFields, flattenedSchemaElements);

                                    rowFieldValues.InsertWithPadding(field.Alias, field.Ordinal, collapsedValue);
                                }
                                else
                                {
                                    throw new KbEngineException("Aggregate function found during scalar materialization.");
                                }
                            }
                        }

                        foreach (var field in query.OrderBy)
                        {
                            /*
                            if (field.Expression is QueryFieldDocumentIdentifier fieldDocumentIdentifier)
                            {
                                var fieldValue = row.SchemaElements[fieldDocumentIdentifier.SchemaAlias][fieldDocumentIdentifier.FieldName];
                                rowFieldValues.InsertWithPadding(field.Alias, field.Ordinal, fieldValue);
                            }
                            else if (field.Expression is IQueryFieldExpression fieldExpression)
                            {
                                if (fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionAggregate>().Any() == false)
                                {
                                    var collapsedValue = StaticScalarExpressionProcessor.CollapseScalarQueryField(
                                        fieldExpression, transaction, query, query.SelectFields, flattenedSchemaElements);

                                    rowFieldValues.InsertWithPadding(field.Alias, field.Ordinal, collapsedValue);
                                }
                                else
                                {
                                    throw new KbEngineException("Aggregate function found during scalar materialization sort.");
                                }
                            }
                            */
                        }

                        lock (childPool)
                        {
                            materializedRowValues.Values.Add(rowFieldValues);
                        }
                    });
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
                    var flattenedSchemaElements = row.SchemaElements.Flatten();

                    var groupKey = new StringBuilder();

                    foreach (var groupField in query.GroupBy)
                    {
                        var collapsedGroupField = groupField.Expression.CollapseScalarQueryField(
                            transaction, query, query.SelectFields, flattenedSchemaElements);

                        groupKey.Append($"[{collapsedGroupField?.ToLowerInvariant()}]");
                    }

                    if (groupRows.TryGetValue(groupKey.ToString(), out var groupRow) == false)
                    {
                        groupRow = new(); //Group does not yet exist, create it.
                        groupRows.Add(groupKey.ToString(), groupRow);

                        //This is where we need to collapse the non aggregated field expressions. We only do this
                        //  when we CREATE the GroupRow because these values should be distinct since we are grouping.
                        //TODO: We should check these fields/expression to make sure that they are either constant or being referenced by the group clause.
                        foreach (var field in query.SelectFields)
                        {
                            if (field.Expression is QueryFieldDocumentIdentifier fieldDocumentIdentifier)
                            {
                                var fieldValue = row.SchemaElements[fieldDocumentIdentifier.SchemaAlias][fieldDocumentIdentifier.FieldName];
                                groupRow.Values.InsertWithPadding(field.Alias, field.Ordinal, fieldValue);
                            }
                            else if (field.Expression is IQueryFieldExpression fieldExpression)
                            {
                                if (fieldExpression.FunctionDependencies.OfType<QueryFieldExpressionFunctionAggregate>().Any() == false)
                                {
                                    var collapsedValue = StaticScalarExpressionProcessor.CollapseScalarQueryField(
                                        fieldExpression, transaction, query, query.SelectFields, flattenedSchemaElements);

                                    groupRow.Values.InsertWithPadding(field.Alias, field.Ordinal, collapsedValue);
                                }
                                else
                                {
                                    groupRow.Values.InsertWithPadding(field.Alias, field.Ordinal, null); //To be filled in with aggregate result later.
                                }
                            }
                        }
                    }

                    foreach (var aggregationFunction in query.SelectFields.AggregationFunctions)
                    {
                        var aggregationArrayParam = aggregationFunction.Function.Parameters.First();

                        //All aggregation parameters are collapsed here at query processing time.
                        var collapsedAggregationParameterValue = aggregationArrayParam.CollapseScalarExpressionFunctionParameter(transaction,
                            query.EnsureNotNull(), query.SelectFields, flattenedSchemaElements, aggregationFunction.FunctionDependencies);

                        if (groupRow.GroupAggregateFunctionParameters.TryGetValue(
                            aggregationFunction.Function.ExpressionKey, out var groupAggregateFunctionParameter) == false)
                        {
                            //Create a new group detail.
                            groupAggregateFunctionParameter = new();

                            //Skip past the required AggregationArray parameter and collapse any supplemental aggregation function parameters.
                            //Supplemental parameters for aggregate functions would be something like "boolean countDistinct" for the count() function.
                            //We only do this when we create the GroupDetail because like the GroupRow, there is only one of these per group, per 
                            foreach (var supplementalParam in aggregationFunction.Function.Parameters.Skip(1))
                            {
                                var collapsedSupplementalParamValue = supplementalParam.CollapseScalarExpressionFunctionParameter(
                                    transaction, query, query.SelectFields, flattenedSchemaElements, new());

                                groupAggregateFunctionParameter.SupplementalParameters.Add(collapsedSupplementalParamValue);
                            }

                            groupRow.GroupAggregateFunctionParameters.Add(aggregationFunction.Function.ExpressionKey, groupAggregateFunctionParameter);
                        }

                        //Keep track of the values that need to be aggregated, these will be passed as the first parameter to the aggregate function.
                        if (collapsedAggregationParameterValue != null)
                        {
                            groupAggregateFunctionParameter.AggregationValues.Add(collapsedAggregationParameterValue);
                        }
                        else
                        {
                            transaction.AddWarning(KbTransactionWarning.AggregateDisqualifiedByNullValue);
                        }
                    }
                }

                var fieldsWithAggregateFunctionCalls = query.SelectFields.FieldsWithAggregateFunctionCalls;

                //The operation.GroupRows contains the resulting row template, with all fields in the correct position,
                //  all that is left to do is execute the aggregation functions and fill in the appropriate fields.
                foreach (var groupRow in groupRows)
                {
                    materializedRowValues.Values.Add(groupRow.Value.Values);

                    foreach (var aggregateFunctionField in fieldsWithAggregateFunctionCalls)
                    {
                        var aggregateExpressionResult = aggregateFunctionField.CollapseAggregateQueryField(
                            transaction, query, groupRow.Value.GroupAggregateFunctionParameters);

                        groupRow.Value.Values.InsertWithPadding(aggregateFunctionField.Alias, aggregateFunctionField.Ordinal, aggregateExpressionResult);
                    }
                }

                #endregion
            }

            return materializedRowValues;
        }

        /// <summary>
        /// Gets a collection of WHERE clause qualified rows, in parallel, from the first schema in th query.
        /// </summary>
        private static SchemaIntersectionRowCollection GatherPrimarySchemaRows(EngineCore core, Transaction transaction,
            QuerySchemaMap schemaMappings, PreparedQuery query, string[]? gatherDocumentsIdsForSchemaPrefixes = null)
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

            //If we do not have any documents, then get the whole schema.
            documentPointers ??= core.Documents.AcquireDocumentPointers(transaction, primarySchema.Value.PhysicalSchema, LockOperation.Read);

            var schemaIntersectionRowCollection = new SchemaIntersectionRowCollection();

            var childThreadPool = core.ThreadPool.Lookup.CreateChildQueue(core.Settings.LookupChildThreadPoolQueueDepth);

            foreach (var documentPointer in documentPointers)
            {
                childThreadPool.Enqueue(() =>
                {
                    var physicalDocument = core.Documents.AcquireDocument(transaction, primarySchema.Value.PhysicalSchema, documentPointer, LockOperation.Read);

                    if (IsWhereClauseMatch(transaction, query, primarySchema.Value.Conditions, physicalDocument.Elements))
                    {
                        //Found a document that matched the where clause, add row to the results collection.
                        var schemaIntersectionRow = new SchemaIntersectionRow();
                        schemaIntersectionRow.SchemaElements.Add(primarySchema.Value.Prefix.ToLowerInvariant(), physicalDocument.Elements);
                        if (gatherDocumentsIdsForSchemaPrefixes?.Contains(primarySchema.Value.Prefix, StringComparer.InvariantCultureIgnoreCase) == true)
                        {
                            schemaIntersectionRow.DocumentPointers.Add(primarySchema.Value.Prefix.ToLowerInvariant(), documentPointer);
                        }

                        lock (schemaIntersectionRowCollection)
                        {
                            schemaIntersectionRowCollection.Add(schemaIntersectionRow);
                        }
                    }
                });
            }

            var ptThreadCompletion = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion);
            childThreadPool.WaitForCompletion();
            ptThreadCompletion?.StopAndAccumulate();

            return schemaIntersectionRowCollection;
        }

        private static SchemaIntersectionRowCollection GatherIntersectedRows(EngineCore core, Transaction transaction,
            QuerySchemaMap schemaMappings, PreparedQuery query, string[]? gatherDocumentsIdsForSchemaPrefixes = null)
        {
            var resultingRowCollection = GatherPrimarySchemaRows(core, transaction, schemaMappings, query, gatherDocumentsIdsForSchemaPrefixes);

            var childThreadPool = core.ThreadPool.Intersection.CreateChildQueue(core.Settings.IntersectionChildThreadPoolQueueDepth);

            foreach (var schemaMap in schemaMappings.Skip(1))
            {
                var schemaIntersectionRowCollection = new SchemaIntersectionRowCollection();

                foreach (var templateRow in resultingRowCollection)
                {
                    childThreadPool.Enqueue(() =>
                    {
                        var templateRowClone = templateRow.Clone();

                        IEnumerable<DocumentPointer>? documentPointers = null;

                        if (schemaMap.Value.Optimization?.IndexingConditionGroup.Count > 0)
                        {
                            var keyValues = new KbInsensitiveDictionary<string?>();

                            //Grab the values from the right-hand-schema join clause and create a lookup of the values.
                            var rightHandDocumentIdentifiers = schemaMap.Value.Optimization.Conditions.FlattenToRightDocumentIdentifiers();
                            foreach (var documentIdentifier in rightHandDocumentIdentifiers)
                            {
                                var documentContent = templateRowClone.SchemaElements[documentIdentifier.SchemaAlias ?? ""];
                                if (!documentContent.TryGetValue(documentIdentifier.FieldName, out string? documentValue))
                                {
                                    throw new KbProcessingException($"Join clause field not found in document: [{schemaMap.Key}].");
                                }
                                keyValues[documentIdentifier.FieldName] = documentValue?.ToString() ?? "";
                            }

                            //We are going to create a limited document catalog using the indexes.
                            var indexMatchedDocuments = core.Indexes.MatchSchemaDocumentsByConditionsClause(
                                schemaMap.Value.PhysicalSchema, schemaMap.Value.Optimization, query, schemaMap.Value.Prefix, keyValues);

                            documentPointers = indexMatchedDocuments.Select(o => o.Value);
                        }

                        //If we do not have any documents, then get the whole schema.
                        documentPointers ??= core.Documents.AcquireDocumentPointers(transaction, schemaMap.Value.PhysicalSchema, LockOperation.Read);

                        foreach (var documentPointer in documentPointers)
                        {
                            var physicalDocument = core.Documents.AcquireDocument(transaction, schemaMap.Value.PhysicalSchema, documentPointer, LockOperation.Read);

                            templateRowClone.SchemaElements[schemaMap.Value.Prefix.ToLowerInvariant()] = physicalDocument.Elements;

                            if (IsJoinExpressionMatch(transaction, query, schemaMap.Value.Conditions, templateRowClone))
                            {
                                //Found a document that matched the where clause, add row to the results collection.
                                if (gatherDocumentsIdsForSchemaPrefixes?.Contains(schemaMap.Value.Prefix, StringComparer.InvariantCultureIgnoreCase) == true)
                                {
                                    templateRowClone.DocumentPointers.Add(schemaMap.Value.Prefix.ToLowerInvariant(), documentPointer);
                                }

                                lock (schemaIntersectionRowCollection)
                                {
                                    schemaIntersectionRowCollection.Add(templateRowClone.Clone());
                                }
                            }
                            else
                            {
                                //TODO: Implement left-outer join.
                            }
                        }
                    });
                }

                var ptThreadCompletion = transaction.Instrumentation.CreateToken(PerformanceCounter.ThreadCompletion);
                childThreadPool.WaitForCompletion();
                ptThreadCompletion?.StopAndAccumulate();

                resultingRowCollection = schemaIntersectionRowCollection.Clone();
            }

            return resultingRowCollection;
        }

        private static bool IsWhereClauseMatch(Transaction transaction, PreparedQuery query,
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

                        matchExpression.Parameters[entry.ExpressionVariable] = entry.IsMatch(transaction, collapsedLeft, collapsedRight);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        /// <summary>
        /// Collapses all left-and-right condition values, compares them, and fills in the expression variables with the comparison result.
        /// </summary>
        private static bool IsJoinExpressionMatch(Transaction transaction,
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

                        matchExpression.Parameters[entry.ExpressionVariable] = entry.IsMatch(transaction, collapsedLeft, collapsedRight);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

    }
}
