﻿using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.QueryProcessing;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers;
using NTDLS.Katzebase.Engine.Sessions;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to documents.
    /// </summary>
    internal class DocumentQueryHandlers
    {
        private readonly EngineCore _core;

        public DocumentQueryHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to instantiate document query handler.", ex);
                throw;
            }
        }

        internal KbQueryDocumentListResult ExecuteSelect(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                var result = StaticSearcherMethods.FindDocumentsByPreparedQuery(_core, transactionReference.Transaction, preparedQuery);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute document select for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryDocumentListResult ExecuteSelectInto(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                var targetSchema = preparedQuery.Attributes[PreparedQuery.QueryAttribute.TargetSchema].ToString();

                var physicalTargetSchema = _core.Schemas.AcquireVirtual(transactionReference.Transaction, targetSchema.EnsureNotNull(), LockOperation.Write);

                if (physicalTargetSchema.Exists == false)
                {
                    _core.Schemas.CreateSingleSchema(transactionReference.Transaction, targetSchema, _core.Settings.DefaultDocumentPageSize);
                    physicalTargetSchema = _core.Schemas.AcquireVirtual(transactionReference.Transaction, targetSchema, LockOperation.Write);
                }

                var result = StaticSearcherMethods.FindDocumentsByPreparedQuery(_core, transactionReference.Transaction, preparedQuery);

                var duplicateFields = result.Fields
                    .GroupBy(o => o.Name)
                    .Where(o => o.Count() > 1).Select(o => o.Key).ToList();

                if (duplicateFields.Count != 0)
                {
                    string fields = "[" + string.Join("],[", duplicateFields) + "]";
                    throw new KbEngineException($"Field(s) {fields} were specified more than once.");
                }

                foreach (var row in result.Rows)
                {
                    var document = new KbInsensitiveDictionary<string>();

                    for (int i = 0; i < result.Fields.Count; i++)
                    {
                        document.Add(result.Fields[i].Name, row.Values[i] ?? string.Empty);
                    }
                    string documentContent = JsonConvert.SerializeObject(document);

                    _core.Documents.InsertDocument(transactionReference.Transaction, physicalTargetSchema, documentContent);
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute document select for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts a document into a schema.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="preparedQuery"></param>
        /// <returns></returns>
        internal KbActionResponse ExecuteInsert(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);

                var physicalSchema = _core.Schemas.Acquire(
                    transactionReference.Transaction, preparedQuery.Schemas.Single().Name, LockOperation.Write);

                if (preparedQuery.InsertFieldValues != null)
                {
                    //Executing a "insert into, values" statement.

                    foreach (var insertFieldValues in preparedQuery.InsertFieldValues)
                    {
                        var keyValuePairs = new KbInsensitiveDictionary<string?>();

                        foreach (var insertValue in insertFieldValues)
                        {
                            var collapsedValue = insertValue.Expression.CollapseScalerQueryField(
                                transactionReference.Transaction, preparedQuery, new(preparedQuery.Batch), new());

                            keyValuePairs.Add(insertValue.Alias, collapsedValue);
                        }

                        var documentContent = JsonConvert.SerializeObject(keyValuePairs);
                        _core.Documents.InsertDocument(transactionReference.Transaction, physicalSchema, documentContent);
                    }

                    return transactionReference.CommitAndApplyMetricsThenReturnResults(preparedQuery.InsertFieldValues.Count);
                }
                else if (preparedQuery.InsertSelectQuery != null)
                {
                    //Executing a "insert into, select from" statement.

                    var results = _core.Query.ExecuteQuery(session, preparedQuery.InsertSelectQuery);

                    if (results.Collection.Count == 0)
                    {
                        return transactionReference.CommitAndApplyMetricsThenReturnResults(0);
                    }
                    else if (results.Collection.Count == 1)
                    {
                        if (results.Collection[0].Fields.Count < preparedQuery.InsertFieldNames.Count)
                        {
                            throw new KbParserException("Values list contains less values than the field list.");
                        }
                        else if (results.Collection[0].Fields.Count > preparedQuery.InsertFieldNames.Count)
                        {
                            throw new KbParserException("Values list contains more values than the field list.");
                        }

                        foreach (var row in results.Collection[0].Rows)
                        {
                            var keyValuePairs = new KbInsensitiveDictionary<string?>();

                            for (int fieldIndex = 0; fieldIndex < results.Collection[0].Fields.Count; fieldIndex++)
                            {
                                keyValuePairs.Add(preparedQuery.InsertFieldNames[fieldIndex], row.Values[fieldIndex]);
                            }

                            var documentContent = JsonConvert.SerializeObject(keyValuePairs);
                            _core.Documents.InsertDocument(transactionReference.Transaction, physicalSchema, documentContent);
                        }

                        return transactionReference.CommitAndApplyMetricsThenReturnResults(results.Collection[0].Rows.Count);
                    }
                    else if (results.Collection.Count > 1)
                    {
                        throw new KbMultipleRecordSetsException("Insert select resulted in more than one result-set.");
                    }
                }

                throw new KbEngineException("Insert statement must be accompanied by a values list or a source select statement.");
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute document insert for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Updates a document in a schema.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="preparedQuery"></param>
        /// <returns></returns>
        internal KbActionResponse ExecuteUpdate(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                var firstSchema = preparedQuery.Schemas.Single();
                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, firstSchema.Name, LockOperation.Read);

                var getDocumentsIdsForSchemaPrefixes = new string[] { firstSchema.Prefix };

                var rowDocumentIdentifiers = StaticSearcherMethods.FindDocumentPointersByPreparedQuery(
                    _core, transactionReference.Transaction, preparedQuery, getDocumentsIdsForSchemaPrefixes);

                var updatedDocumentPointers = new List<DocumentPointer>();

                foreach (var rowDocumentIdentifier in rowDocumentIdentifiers)
                {
                    var physicalDocument = _core.Documents.AcquireDocument
                        (transactionReference.Transaction, physicalSchema, rowDocumentIdentifier.DocumentPointer, LockOperation.Write);

                    foreach (var updateValue in preparedQuery.UpdateFieldValues.EnsureNotNull())
                    {
                        var collapsedValue = updateValue.Expression.CollapseScalerQueryField(
                            transactionReference.Transaction, preparedQuery, preparedQuery.UpdateFieldValues, rowDocumentIdentifier.AuxiliaryFields);

                        if (physicalDocument.Elements.ContainsKey(updateValue.Alias))
                        {
                            physicalDocument.Elements[updateValue.Alias] = collapsedValue;
                        }
                        else
                        {
                            physicalDocument.Elements.Add(updateValue.Alias, collapsedValue);
                        }
                    }

                    updatedDocumentPointers.Add(rowDocumentIdentifier.DocumentPointer);
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(rowDocumentIdentifiers.Count());
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute document update for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryDocumentListResult ExecuteSample(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                string schemaName = preparedQuery.Schemas.Single().Name;
                var result = StaticSearcherMethods.SampleSchemaDocuments(
                    _core, transactionReference.Transaction, schemaName, preparedQuery.RowLimit);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute document sample for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryDocumentListResult ExecuteList(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                string schemaName = preparedQuery.Schemas.Single().Name;
                var result = StaticSearcherMethods.ListSchemaDocuments(
                    _core, transactionReference.Transaction, schemaName, preparedQuery.RowLimit);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute document list for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryExplain ExecuteExplainPlan(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);

                var result = new KbQueryExplain();

                foreach (var schema in preparedQuery.Schemas)
                {
                    if (schema.Conditions != null)
                    {
                        var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, schema.Name, LockOperation.Read);

                        var lookupOptimization = IndexingConditionOptimization.BuildTree(_core,
                            transactionReference.Transaction, preparedQuery, physicalSchema, schema.Conditions, schema.Prefix);

                        var explanation = IndexingConditionOptimization.ExplainPlan(physicalSchema, lookupOptimization, preparedQuery, schema.Prefix);

                        transactionReference.Transaction.AddMessage(explanation, KbMessageType.Explain);
                    }
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute document explain for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryExplain ExecuteExplainOperations(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);

                var result = new KbQueryExplain();

                foreach (var schema in preparedQuery.Schemas)
                {
                    if (schema.Conditions != null)
                    {
                        var explanation = schema.Conditions.ExplainOperations();
                        transactionReference.Transaction.AddMessage(explanation, KbMessageType.Explain);
                    }
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute document explain for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDelete(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                var firstSchema = preparedQuery.Schemas.First();

                using var transactionReference = _core.Transactions.Acquire(session);

                var rowDocumentIdentifiers = StaticSearcherMethods.FindDocumentPointersByPreparedQuery(
                    _core, transactionReference.Transaction, preparedQuery, [firstSchema.Prefix]);

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, firstSchema.Name, LockOperation.Delete);

                _core.Documents.DeleteDocuments(transactionReference.Transaction, physicalSchema, rowDocumentIdentifiers.Select(o => o.DocumentPointer));

                return transactionReference.CommitAndApplyMetricsThenReturnResults(rowDocumentIdentifiers.Count());
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute document delete for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
