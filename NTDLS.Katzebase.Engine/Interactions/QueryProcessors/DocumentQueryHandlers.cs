using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Expressions;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers;
using NTDLS.Katzebase.PersistentTypes.Document;
using System.Diagnostics;
using static NTDLS.Katzebase.Api.KbConstants;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.QueryProcessors
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
                LogManager.Error($"Failed to instantiate document query handler.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteSelect(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                foreach (var schema in query.Schemas)
                {
                    _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schema.Name, SecurityPolicyPermission.Read);
                }

                #endregion

                var result = StaticSearcherProcessor.FindDocumentsByQuery(_core, transactionReference.Transaction, query);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteSelectInto(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var targetSchema = query.GetAttribute<string>(PreparedQuery.Attribute.TargetSchemaName);

                #region Security policy enforcment.

                foreach (var schema in query.Schemas)
                {
                    _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schema.Name, SecurityPolicyPermission.Read);
                }

                //For "SELECT INTO", we require "Manage" because "SELECT INTO" can create schemas.
                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, targetSchema, SecurityPolicyPermission.Manage);

                #endregion

                var physicalTargetSchema = _core.Schemas.AcquireVirtual(transactionReference.Transaction, targetSchema.EnsureNotNull(), LockOperation.Write, LockOperation.Read);
                if (physicalTargetSchema.Exists == false)
                {
                    _core.Schemas.CreateSingleSchema(transactionReference.Transaction, targetSchema, _core.Settings.DefaultDocumentPageSize);
                    physicalTargetSchema = _core.Schemas.AcquireVirtual(transactionReference.Transaction, targetSchema, LockOperation.Write, LockOperation.Read);
                }

                var result = StaticSearcherProcessor.FindDocumentsByQuery(_core, transactionReference.Transaction, query);

                var duplicateFields = result.Fields
                    .GroupBy(o => o.Name)
                    .Where(o => o.Count() > 1).Select(o => o.Key).ToList();

                if (duplicateFields.Count != 0)
                {
                    throw new KbProcessingException($"Field(s) [{string.Join("],[", duplicateFields)}] were specified more than once.");
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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts a document into a schema.
        /// </summary>
        internal KbActionResponse ExecuteInsert(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var targetSchema = query.GetAttribute<string>(PreparedQuery.Attribute.TargetSchemaName);

                #region Security policy enforcment.

                foreach (var schema in query.Schemas)
                {
                    _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schema.Name, SecurityPolicyPermission.Read);
                }

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, targetSchema, SecurityPolicyPermission.Write);

                #endregion

                var physicalSchema = _core.Schemas.Acquire(
                    transactionReference.Transaction, targetSchema, LockOperation.Write);

                if (query.InsertFieldValues != null)
                {
                    //Executing a "insert into, values" statement.

                    foreach (var insertFieldValues in query.InsertFieldValues)
                    {
                        var keyValuePairs = new KbInsensitiveDictionary<string?>();

                        foreach (var insertValue in insertFieldValues)
                        {
                            var collapsedValue = insertValue.Expression.CollapseScalarQueryField(
                                transactionReference.Transaction, query, new(query.Batch), new());

                            keyValuePairs.Add(insertValue.Alias, collapsedValue);
                        }

                        var documentContent = JsonConvert.SerializeObject(keyValuePairs);
                        _core.Documents.InsertDocument(transactionReference.Transaction, physicalSchema, documentContent);
                    }

                    return transactionReference.CommitAndApplyMetricsThenReturnResults(query.InsertFieldValues.Count);
                }
                else if (query.InsertSelectQuery != null)
                {
                    //Executing a "insert into, select from" statement.

                    var results = _core.Query.ExecuteQuery(session, query.InsertSelectQuery);

                    if (results.Collection.Count == 0)
                    {
                        return transactionReference.CommitAndApplyMetricsThenReturnResults(0);
                    }
                    else if (results.Collection.Count == 1)
                    {
                        if (results.Collection[0].Fields.Count < query.InsertFieldNames.Count)
                        {
                            throw new KbProcessingException("Values list contains less values than the field list.");
                        }
                        else if (results.Collection[0].Fields.Count > query.InsertFieldNames.Count)
                        {
                            throw new KbProcessingException("Values list contains more values than the field list.");
                        }

                        foreach (var row in results.Collection[0].Rows)
                        {
                            var keyValuePairs = new KbInsensitiveDictionary<string?>();

                            for (int fieldIndex = 0; fieldIndex < results.Collection[0].Fields.Count; fieldIndex++)
                            {
                                keyValuePairs.Add(query.InsertFieldNames[fieldIndex], row.Values[fieldIndex]);
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

                throw new KbProcessingException("Insert statement must be accompanied by a values list or a source select statement.");
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Updates a documents in a schema based on where clause and join conditions.
        /// </summary>
        internal KbActionResponse ExecuteUpdate(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var targetSchemaAlias = query.GetAttribute<string>(PreparedQuery.Attribute.TargetSchemaAlias);
                var targetSchema = query.Schemas.Where(o => o.Alias.Is(targetSchemaAlias)).Single();

                #region Security policy enforcment.

                foreach (var schema in query.Schemas)
                {
                    _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schema.Name, SecurityPolicyPermission.Read);
                }

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, targetSchema.Name, SecurityPolicyPermission.Write);

                #endregion

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, targetSchema.Name, LockOperation.Read);

                var gatherDocumentPointersForSchemaAliases = new List<string>() { targetSchemaAlias };

                var schemaIntersectionRowDocumentIdentifierCollection = StaticSearcherProcessor.FindDocumentPointersByQuery(
                    _core, transactionReference.Transaction, query, gatherDocumentPointersForSchemaAliases);

                var updatedDocuments = new Dictionary<DocumentPointer, KbInsensitiveDictionary<string?>>();

                foreach (var schemaIntersectionRowDocumentIdentifier in schemaIntersectionRowDocumentIdentifierCollection)
                {
                    var schemaElements = schemaIntersectionRowDocumentIdentifier.Value.Flatten();

                    var modifiedElements = new KbInsensitiveDictionary<string?>();

                    foreach (var updateValue in query.UpdateFieldValues.EnsureNotNull())
                    {
                        var collapsedValue = updateValue.Expression.CollapseScalarQueryField(
                            transactionReference.Transaction, query, query.UpdateFieldValues, schemaElements);

                        modifiedElements.Add(updateValue.Alias, collapsedValue);
                    }

                    updatedDocuments.Add(schemaIntersectionRowDocumentIdentifier.Key, modifiedElements);
                }

                _core.Documents.UpdateDocuments(transactionReference.Transaction, physicalSchema, updatedDocuments);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(schemaIntersectionRowDocumentIdentifierCollection.Count);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteSample(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                string schemaName = query.Schemas.Single().Name;

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schemaName, SecurityPolicyPermission.Read);

                #endregion

                var result = StaticSearcherProcessor.SampleSchemaDocuments(
                    _core, transactionReference.Transaction, schemaName, query.RowLimit);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteList(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                string schemaName = query.Schemas.Single().Name;

                #region Security policy enforcment.

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schemaName, SecurityPolicyPermission.Write);

                #endregion

                var result = StaticSearcherProcessor.ListSchemaDocuments(
                    _core, transactionReference.Transaction, schemaName, query.RowLimit);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryExplain ExecuteExplainPlan(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var result = new KbQueryExplain();

                #region Security policy enforcment.

                foreach (var schema in query.Schemas)
                {
                    _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schema.Name, SecurityPolicyPermission.Manage);
                }

                #endregion

                foreach (var schema in query.Schemas)
                {
                    if (schema.Conditions != null)
                    {
                        var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, schema.Name, LockOperation.Read);

                        var lookupOptimization = IndexingConditionOptimization.SelectUsableIndexes(_core,
                            transactionReference.Transaction, query, physicalSchema, schema.Conditions, schema.Alias);

                        var explanation = IndexingConditionOptimization.ExplainPlan(physicalSchema, lookupOptimization, query, schema.Alias);

                        transactionReference.Transaction.AddMessage(explanation, KbMessageType.Explain);
                    }
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbQueryExplain ExecuteExplainOperations(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var result = new KbQueryExplain();

                #region Security policy enforcment.

                foreach (var schema in query.Schemas)
                {
                    _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schema.Name, SecurityPolicyPermission.Manage);
                }

                #endregion

                foreach (var schema in query.Schemas)
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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDelete(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                var targetSchemaAlias = query.GetAttribute<string>(PreparedQuery.Attribute.TargetSchemaAlias);
                var targetSchema = query.Schemas.Where(o => o.Alias.Is(targetSchemaAlias)).Single();

                #region Security policy enforcment.

                foreach (var schema in query.Schemas)
                {
                    _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, schema.Name, SecurityPolicyPermission.Read);
                }

                _core.Policy.EnforceSchemaPolicy(transactionReference.Transaction, targetSchema.Name, SecurityPolicyPermission.Write);

                #endregion

                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, targetSchema.Name, LockOperation.Delete);

                var gatherDocumentPointersForSchemaAliases = new List<string>() { targetSchemaAlias };

                var schemaIntersectionRowDocumentIdentifierCollection = StaticSearcherProcessor.FindDocumentPointersByQuery(
                    _core, transactionReference.Transaction, query, gatherDocumentPointersForSchemaAliases);

                var documentsToDelete = new HashSet<DocumentPointer>();

                foreach (var schemaIntersectionRowDocumentIdentifier in schemaIntersectionRowDocumentIdentifierCollection)
                {
                    documentsToDelete.Add(schemaIntersectionRowDocumentIdentifier.Key);
                }

                _core.Documents.DeleteDocuments(transactionReference.Transaction, physicalSchema, documentsToDelete);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(schemaIntersectionRowDocumentIdentifierCollection.Count);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
