using Newtonsoft.Json;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Functions.Parameters;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Query;
using NTDLS.Katzebase.Engine.Query.Searchers;
using NTDLS.Katzebase.Engine.Sessions;
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
                _core.Log.Write($"Failed to instantiate document query handler.", ex);
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
                _core.Log.Write($"Failed to execute document select for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryDocumentListResult ExecuteSelectInto(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                var targetSchema = preparedQuery.Attributes[PreparedQuery.QueryAttribute.TargetSchema].ToString();
                KbUtility.EnsureNotNull(targetSchema);

                var physicalTargetSchema = _core.Schemas.AcquireVirtual(transactionReference.Transaction, targetSchema, LockOperation.Write);

                if (physicalTargetSchema.Exists == false)
                {
                    _core.Schemas.CreateSingleSchema(transactionReference.Transaction, targetSchema, _core.Settings.DefaultDocumentPageSize);
                    physicalTargetSchema = _core.Schemas.AcquireVirtual(transactionReference.Transaction, targetSchema, LockOperation.Write);
                }

                var result = StaticSearcherMethods.FindDocumentsByPreparedQuery(_core, transactionReference.Transaction, preparedQuery);

                var duplicateFields = result.Fields.GroupBy(o => o.Name).Where(o => o.Count() > 1).Select(o => o.Key).ToList();

                if (duplicateFields.Any())
                {
                    string fields = "[" + string.Join("],[", duplicateFields) + "]";
                    throw new KbEngineException($"The field(s) {fields} was specified more than once.");
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
                _core.Log.Write($"Failed to execute document select for process id {session.ProcessId}.", ex);
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
                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, preparedQuery.Schemas.Single().Name, LockOperation.Write);

                foreach (var upsertValues in preparedQuery.UpsertValues)
                {
                    var keyValuePairs = new KbInsensitiveDictionary<string?>();

                    foreach (var updateValue in upsertValues)
                    {
                        string? fieldValue = string.Empty;

                        //Execute functions
                        if (updateValue.Value is FunctionWithParams || updateValue.Value is FunctionExpression)
                        {
                            fieldValue = ScalerFunctionImplementation.CollapseAllFunctionParameters(transactionReference.Transaction, updateValue.Value, new KbInsensitiveDictionary<string?>());
                        }
                        else if (updateValue.Value is FunctionConstantParameter)
                        {
                            fieldValue = ((FunctionConstantParameter)updateValue.Value).RawValue;
                        }
                        else
                        {
                            throw new KbNotImplementedException($"The function type {updateValue.Value.GetType().Name} is not implemented.");
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
                _core.Log.Write($"Failed to execute document insert for process id {session.ProcessId}.", ex);
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

                if (preparedQuery.Attributes.ContainsKey(PreparedQuery.QueryAttribute.SpecificSchemaPrefix))
                {
                    getDocumentPointsForSchemaPrefix = preparedQuery.Attributes[PreparedQuery.QueryAttribute.SpecificSchemaPrefix] as string;
                }

                KbUtility.EnsureNotNull(getDocumentPointsForSchemaPrefix);

                var documentPointers = StaticSearcherMethods.FindDocumentPointersByPreparedQuery(_core, transactionReference.Transaction, preparedQuery, getDocumentPointsForSchemaPrefix);

                var updatedDocumentPointers = new List<DocumentPointer>();

                foreach (var documentPointer in documentPointers)
                {
                    var physicalDocument = _core.Documents.AcquireDocument(transactionReference.Transaction, physicalSchema, documentPointer, LockOperation.Write);
                    KbUtility.EnsureNotNull(physicalDocument);

                    foreach (var updateValue in preparedQuery.UpdateValues)
                    {
                        string? fieldValue = string.Empty;

                        //Execute functions
                        if (updateValue.Value is FunctionWithParams || updateValue.Value is FunctionExpression)
                        {
                            fieldValue = ScalerFunctionImplementation.CollapseAllFunctionParameters(transactionReference.Transaction, updateValue.Value, physicalDocument.Elements);
                        }
                        else if (updateValue.Value is FunctionConstantParameter)
                        {
                            fieldValue = ((FunctionConstantParameter)updateValue.Value).RawValue;
                        }
                        else
                        {
                            throw new KbNotImplementedException($"The function type {updateValue.Value.GetType().Name} is not implemented.");
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
                _core.Log.Write($"Failed to execute document update for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryDocumentListResult ExecuteSample(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                string schemaName = preparedQuery.Schemas.Single().Name;
                var result = StaticSearcherMethods.SampleSchemaDocuments(_core, transactionReference.Transaction, schemaName, preparedQuery.RowLimit);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute document sample for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryDocumentListResult ExecuteList(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                string schemaName = preparedQuery.Schemas.Single().Name;
                var result = StaticSearcherMethods.ListSchemaDocuments(_core, transactionReference.Transaction, schemaName, preparedQuery.RowLimit);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute document list for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryDocumentListResult ExecuteExplain(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                throw new KbNotImplementedException();
                /*
                using var transaction = core.Transactions.Begin(processId);
                var physicalSchema = core.Schemas.Acquire(transaction, preparedQuery.Schemas[0].n, LockOperation.Read);

                var lookupOptimization = ConditionLookupOptimization.Build(core, transaction, physicalSchema, preparedQuery.Conditions);
                result.Explanation = lookupOptimization.BuildFullVirtualExpression();

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
                */
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute document explain for process id {session.ProcessId}.", ex);
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

                if (preparedQuery.Attributes.ContainsKey(PreparedQuery.QueryAttribute.SpecificSchemaPrefix))
                {
                    getDocumentPointsForSchemaPrefix = preparedQuery.Attributes[PreparedQuery.QueryAttribute.SpecificSchemaPrefix] as string;
                }

                KbUtility.EnsureNotNull(getDocumentPointsForSchemaPrefix);

                var documentPointers = StaticSearcherMethods.FindDocumentPointersByPreparedQuery(_core, transactionReference.Transaction, preparedQuery, getDocumentPointsForSchemaPrefix);
                _core.Documents.DeleteDocuments(transactionReference.Transaction, physicalSchema, documentPointers.ToArray());
                return transactionReference.CommitAndApplyMetricsThenReturnResults(documentPointers.Count());
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute document delete for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
