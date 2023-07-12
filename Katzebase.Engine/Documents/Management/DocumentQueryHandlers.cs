using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Searchers;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
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
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = StaticSearcherMethods.FindDocumentsByPreparedQuery(core, transaction, preparedQuery);

                    transaction.Commit();
                    result.RowCount = result.Rows.Count;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
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
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var targetSchema = preparedQuery.Attributes[PreparedQuery.QueryAttribute.TargetSchema].ToString();
                    KbUtility.EnsureNotNull(targetSchema);

                    var physicalTargetSchema = core.Schemas.AcquireVirtual(transaction, targetSchema, LockOperation.Write);

                    if (physicalTargetSchema.Exists == false)
                    {
                        core.Schemas.CreateSingleSchema(transaction, targetSchema);
                        physicalTargetSchema = core.Schemas.AcquireVirtual(transaction, targetSchema, LockOperation.Write);
                    }

                    var result = StaticSearcherMethods.FindDocumentsByPreparedQuery(core, transaction, preparedQuery);

                    var duplicateFields = result.Fields.GroupBy(o => o.Name).Where(o => o.Count() > 1).Select(o => o.Key).ToList();

                    if (duplicateFields.Any())
                    {
                        string fields = "[" + string.Join("],[", duplicateFields) + "]";
                        throw new KbEngineException($"The field(s) {fields} was specified more than once.");
                    }

                    foreach (var row in result.Rows)
                    {
                        var document = new Dictionary<string, string>();

                        for (int i = 0; i < result.Fields.Count; i++)
                        {
                            document.Add(result.Fields[i].Name, row.Values[i] ?? string.Empty);
                        }
                        string documentContent = JsonConvert.SerializeObject(document);

                        core.Documents.InsertDocument(transaction, physicalTargetSchema, documentContent);
                    }

                    transaction.Commit();
                    result.RowCount = result.Rows.Count;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
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
        internal KbQueryResult ExecuteInsert(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = new KbQueryResult();
                    var physicalSchema = core.Schemas.Acquire(transaction, preparedQuery.Schemas.Single().Name, LockOperation.Write);

                    foreach (var upsertValues in preparedQuery.UpsertValues)
                    {
                        var keyValuePairs = upsertValues.ToDictionary(o => o.Field.Field, o => o.Value.Value);
                        var documentContent = JsonConvert.SerializeObject(keyValuePairs);
                        core.Documents.InsertDocument(transaction, physicalSchema, documentContent);
                    }

                    transaction.Commit();
                    result.RowCount = preparedQuery.UpsertValues.Count;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
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
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();
                    var firstSchema = preparedQuery.Schemas.Single();
                    var physicalSchema = core.Schemas.Acquire(transaction, firstSchema.Name, LockOperation.Read);

                    var getDocumentPointsForSchemaPrefix = firstSchema.Prefix;

                    if (preparedQuery.Attributes.ContainsKey(PreparedQuery.QueryAttribute.SpecificSchemaPrefix))
                    {
                        getDocumentPointsForSchemaPrefix = preparedQuery.Attributes[PreparedQuery.QueryAttribute.SpecificSchemaPrefix] as string;
                    }

                    KbUtility.EnsureNotNull(getDocumentPointsForSchemaPrefix);

                    var documentPointers = StaticSearcherMethods.FindDocumentPointersByPreparedQuery(core, transaction, preparedQuery, getDocumentPointsForSchemaPrefix);

                    foreach (var upsertValues in preparedQuery.UpsertValues)
                    {
                        //var keyValuePairs = upsertValues.ToDictionary(o => o.Field.Field, o => o.Value.Value);
                        //var documentContent = JsonConvert.SerializeObject(keyValuePairs);
                        //core.Documents.InsertDocument(transaction, physicalSchema, documentContent);
                    }

                    throw new KbNotImplementedException("UPDATE is not implemented");

                    transaction.Commit();
                    result.RowCount = preparedQuery.UpsertValues.Count;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
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
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    string schemaName = preparedQuery.Schemas.Single().Name;
                    var result = StaticSearcherMethods.SampleSchemaDocuments(core, transaction, schemaName, preparedQuery.RowLimit);

                    transaction.Commit();
                    result.RowCount = result.Rows.Count;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
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
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    string schemaName = preparedQuery.Schemas.Single().Name;
                    var result = StaticSearcherMethods.ListSchemaDocuments(core, transaction, schemaName, preparedQuery.RowLimit);

                    transaction.Commit();
                    result.RowCount = result.Rows.Count;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
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
                using (var transaction = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(transaction, preparedQuery.Schemas[0].n, LockOperation.Read);

                    var lookupOptimization = ConditionLookupOptimization.Build(core, transaction, physicalSchema, preparedQuery.Conditions);
                    result.Explanation = lookupOptimization.BuildFullVirtualExpression();

                    transaction.Commit();
                    result.RowCount = 0;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                return result;
                }
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
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();
                    var firstSchema = preparedQuery.Schemas.Single();
                    var physicalSchema = core.Schemas.Acquire(transaction, firstSchema.Name, LockOperation.Read);

                    var getDocumentPointsForSchemaPrefix = firstSchema.Prefix;

                    if (preparedQuery.Attributes.ContainsKey(PreparedQuery.QueryAttribute.SpecificSchemaPrefix))
                    {
                        getDocumentPointsForSchemaPrefix = preparedQuery.Attributes[PreparedQuery.QueryAttribute.SpecificSchemaPrefix] as string;
                    }

                    KbUtility.EnsureNotNull(getDocumentPointsForSchemaPrefix);

                    var documentPointers = StaticSearcherMethods.FindDocumentPointersByPreparedQuery(core, transaction, preparedQuery, getDocumentPointsForSchemaPrefix);

                    core.Documents.DeleteDocuments(transaction, physicalSchema, documentPointers.ToArray());

                    transaction.Commit();
                    result.RowCount = documentPointers.Count();
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute document delete for process id {processId}.", ex);
                throw;
            }
        }
    }
}
