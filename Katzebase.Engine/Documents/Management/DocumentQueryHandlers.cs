﻿using Katzebase.Engine.Functions.Parameters;
using Katzebase.Engine.Functions.Scaler;
using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Searchers;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Katzebase.PublicLibrary.Types;
using Newtonsoft.Json;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Documents.Management
{
    /// <summary>
    /// Internal class methods for handling query requests related to documents.
    /// </summary>
    internal class DocumentQueryHandlers
    {
        private readonly Core core;

        public DocumentQueryHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate document query handler.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteSelect(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                var result = StaticSearcherMethods.FindDocumentsByPreparedQuery(core, transactionReference.Transaction, preparedQuery);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute document select for process id {processId}.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteSelectInto(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                var targetSchema = preparedQuery.Attributes[PreparedQuery.QueryAttribute.TargetSchema].ToString();
                KbUtility.EnsureNotNull(targetSchema);

                var physicalTargetSchema = core.Schemas.AcquireVirtual(transactionReference.Transaction, targetSchema, LockOperation.Write);

                if (physicalTargetSchema.Exists == false)
                {
                    core.Schemas.CreateSingleSchema(transactionReference.Transaction, targetSchema, core.Settings.DefaultDocumentPageSize);
                    physicalTargetSchema = core.Schemas.AcquireVirtual(transactionReference.Transaction, targetSchema, LockOperation.Write);
                }

                var result = StaticSearcherMethods.FindDocumentsByPreparedQuery(core, transactionReference.Transaction, preparedQuery);

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

                    core.Documents.InsertDocument(transactionReference.Transaction, physicalTargetSchema, documentContent);
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute document select for process id {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts a document into a schema.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="preparedQuery"></param>
        /// <returns></returns>
        internal KbActionResponse ExecuteInsert(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                var physicalSchema = core.Schemas.Acquire(transactionReference.Transaction, preparedQuery.Schemas.Single().Name, LockOperation.Write);

                foreach (var upsertValues in preparedQuery.UpsertValues)
                {
                    var keyValuePairs = new KbInsensitiveDictionary<string?>();

                    foreach (var updateValue in upsertValues)
                    {
                        string? fieldValue = string.Empty;

                        //Execute functions
                        if (updateValue.Value is FunctionWithParams || updateValue.Value is FunctionExpression)
                        {
                            fieldValue = ScalerFunctionImplementation.CollapseAllFunctionParameters(updateValue.Value, new KbInsensitiveDictionary<string?>());
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
                    core.Documents.InsertDocument(transactionReference.Transaction, physicalSchema, documentContent);
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(preparedQuery.UpsertValues.Count);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute document insert for process id {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Updates a document in a schema.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="preparedQuery"></param>
        /// <returns></returns>
        internal KbActionResponse ExecuteUpdate(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                var firstSchema = preparedQuery.Schemas.Single();
                var physicalSchema = core.Schemas.Acquire(transactionReference.Transaction, firstSchema.Name, LockOperation.Read);

                var getDocumentPointsForSchemaPrefix = firstSchema.Prefix;

                if (preparedQuery.Attributes.ContainsKey(PreparedQuery.QueryAttribute.SpecificSchemaPrefix))
                {
                    getDocumentPointsForSchemaPrefix = preparedQuery.Attributes[PreparedQuery.QueryAttribute.SpecificSchemaPrefix] as string;
                }

                KbUtility.EnsureNotNull(getDocumentPointsForSchemaPrefix);

                var documentPointers = StaticSearcherMethods.FindDocumentPointersByPreparedQuery(core, transactionReference.Transaction, preparedQuery, getDocumentPointsForSchemaPrefix);

                var updatedDocumentPointers = new List<DocumentPointer>();

                foreach (var documentPointer in documentPointers)
                {
                    var physicalDocument = core.Documents.AcquireDocument(transactionReference.Transaction, physicalSchema, documentPointer, LockOperation.Write);
                    KbUtility.EnsureNotNull(physicalDocument);

                    foreach (var updateValue in preparedQuery.UpdateValues)
                    {
                        string? fieldValue = string.Empty;

                        //Execute functions
                        if (updateValue.Value is FunctionWithParams || updateValue.Value is FunctionExpression)
                        {
                            fieldValue = ScalerFunctionImplementation.CollapseAllFunctionParameters(updateValue.Value, physicalDocument.Dictionary);
                        }
                        else if (updateValue.Value is FunctionConstantParameter)
                        {
                            fieldValue = ((FunctionConstantParameter)updateValue.Value).RawValue;
                        }
                        else
                        {
                            throw new KbNotImplementedException($"The function type {updateValue.Value.GetType().Name} is not implemented.");
                        }

                        if (physicalDocument.Dictionary.ContainsKey(updateValue.Key))
                        {
                            physicalDocument.Dictionary[updateValue.Key] = fieldValue;
                        }
                        else
                        {
                            physicalDocument.Dictionary.Add(updateValue.Key, fieldValue);
                        }
                    }

                    updatedDocumentPointers.Add(documentPointer);
                }

                var listOfModifiedFields = preparedQuery.UpdateValues.Select(o => o.Key);

                //We update all of the documents all at once so we dont have to keep opening/closing catalogs.
                core.Documents.UpdateDocuments(transactionReference.Transaction, physicalSchema, updatedDocumentPointers, listOfModifiedFields);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(documentPointers.Count());
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute document update for process id {processId}.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteSample(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                string schemaName = preparedQuery.Schemas.Single().Name;
                var result = StaticSearcherMethods.SampleSchemaDocuments(core, transactionReference.Transaction, schemaName, preparedQuery.RowLimit);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute document sample for process id {processId}.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteList(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                string schemaName = preparedQuery.Schemas.Single().Name;
                var result = StaticSearcherMethods.ListSchemaDocuments(core, transactionReference.Transaction, schemaName, preparedQuery.RowLimit);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute document list for process id {processId}.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteExplain(ulong processId, PreparedQuery preparedQuery)
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
                core.Log.Write($"Failed to execute document explain for process id {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDelete(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                var firstSchema = preparedQuery.Schemas.Single();
                var physicalSchema = core.Schemas.Acquire(transactionReference.Transaction, firstSchema.Name, LockOperation.Read);
                var getDocumentPointsForSchemaPrefix = firstSchema.Prefix;

                if (preparedQuery.Attributes.ContainsKey(PreparedQuery.QueryAttribute.SpecificSchemaPrefix))
                {
                    getDocumentPointsForSchemaPrefix = preparedQuery.Attributes[PreparedQuery.QueryAttribute.SpecificSchemaPrefix] as string;
                }

                KbUtility.EnsureNotNull(getDocumentPointsForSchemaPrefix);

                var documentPointers = StaticSearcherMethods.FindDocumentPointersByPreparedQuery(core, transactionReference.Transaction, preparedQuery, getDocumentPointsForSchemaPrefix);
                core.Documents.DeleteDocuments(transactionReference.Transaction, physicalSchema, documentPointers.ToArray());
                return transactionReference.CommitAndApplyMetricsThenReturnResults(documentPointers.Count());
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute document delete for process id {processId}.", ex);
                throw;
            }
        }
    }
}
