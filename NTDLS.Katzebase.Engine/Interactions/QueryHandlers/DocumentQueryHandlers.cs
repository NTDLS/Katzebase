using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Functions.Parameters;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
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

                foreach (var upsertValues in preparedQuery.UpsertValues)
                {
                    var keyValuePairs = new KbInsensitiveDictionary<string?>();

                    foreach (var updateValue in upsertValues)
                    {
                        string? fieldValue = string.Empty;

                        //Execute functions
                        if (updateValue.Value is FunctionWithParams || updateValue.Value is FunctionExpression)
                        {
                            //TODO: Reimplement scaler functions for insert.
                            //fieldValue = ScalerFunctionImplementation.CollapseAllFunctionParameters(
                            //    transactionReference.Transaction, updateValue.Value, new KbInsensitiveDictionary<string?>());
                            throw new NotImplementedException("Reimplement scaler functions for update statements");
                        }
                        else if (updateValue.Value is FunctionConstantParameter functionConstantParameter)
                        {
                            fieldValue = functionConstantParameter.RawValue;
                        }
                        else
                        {
                            throw new KbNotImplementedException($"Function type {updateValue.Value.GetType().Name} is not implemented.");
                        }

                        keyValuePairs.Add(updateValue.Key, fieldValue);
                    }

                    var documentContent = JsonConvert.SerializeObject(keyValuePairs);
                    _core.Documents.InsertDocument(transactionReference.Transaction, physicalSchema, documentContent);
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(preparedQuery.UpsertValues.Count);
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

                var getDocumentPointsForSchemaPrefix = firstSchema.Prefix;

                if (preparedQuery.Attributes.TryGetValue(PreparedQuery.QueryAttribute.SpecificSchemaPrefix, out object? value))
                {
                    getDocumentPointsForSchemaPrefix = value as string;
                }

                var documentPointers = StaticSearcherMethods.FindDocumentPointersByPreparedQuery(
                    _core, transactionReference.Transaction, preparedQuery, getDocumentPointsForSchemaPrefix.EnsureNotNull());

                var updatedDocumentPointers = new List<DocumentPointer>();

                foreach (var documentPointer in documentPointers)
                {
                    var physicalDocument = _core.Documents.AcquireDocument
                        (transactionReference.Transaction, physicalSchema, documentPointer, LockOperation.Write);

                    foreach (var updateValue in preparedQuery.UpdateValues)
                    {
                        string? fieldValue = string.Empty;

                        //Execute functions
                        if (updateValue.Value is FunctionWithParams || updateValue.Value is FunctionExpression)
                        {
                            //fieldValue = ScalerFunctionImplementation.CollapseAllFunctionParameters(transactionReference.Transaction, updateValue.Value, physicalDocument.Elements);
                            throw new NotImplementedException("Reimplement scaler functions for update statements");
                        }
                        else if (updateValue.Value is FunctionConstantParameter functionConstantParameter)
                        {
                            fieldValue = functionConstantParameter.RawValue;
                        }
                        else
                        {
                            throw new KbNotImplementedException($"Function type {updateValue.Value.GetType().Name} is not implemented.");
                        }

                        if (physicalDocument.Elements.ContainsKey(updateValue.Key))
                        {
                            physicalDocument.Elements[updateValue.Key] = fieldValue;
                        }
                        else
                        {
                            physicalDocument.Elements.Add(updateValue.Key, fieldValue);
                        }
                    }

                    updatedDocumentPointers.Add(documentPointer);
                }

                var listOfModifiedFields = preparedQuery.UpdateValues.Select(o => o.Key);

                //We update all of the documents all at once so we don't have to keep opening/closing catalogs.
                _core.Documents.UpdateDocuments(transactionReference.Transaction, physicalSchema, updatedDocumentPointers, listOfModifiedFields);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(documentPointers.Count());
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
                            transactionReference.Transaction, physicalSchema, schema.Conditions, schema.Prefix);

                        var explanation = lookupOptimization.ExplainPlan(_core, physicalSchema, lookupOptimization, schema.Prefix);

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
                using var transactionReference = _core.Transactions.Acquire(session);
                var firstSchema = preparedQuery.Schemas.Single();
                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, firstSchema.Name, LockOperation.Read);
                var getDocumentPointsForSchemaPrefix = firstSchema.Prefix;

                if (preparedQuery.Attributes.TryGetValue(PreparedQuery.QueryAttribute.SpecificSchemaPrefix, out object? value))
                {
                    getDocumentPointsForSchemaPrefix = value as string;
                }

                var documentPointers = StaticSearcherMethods.FindDocumentPointersByPreparedQuery
                    (_core, transactionReference.Transaction, preparedQuery, getDocumentPointsForSchemaPrefix.EnsureNotNull());
                _core.Documents.DeleteDocuments(transactionReference.Transaction, physicalSchema, documentPointers.ToArray());
                return transactionReference.CommitAndApplyMetricsThenReturnResults(documentPointers.Count());
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute document delete for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
