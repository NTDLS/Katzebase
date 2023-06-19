using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Searchers;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Documents.Management
{
    internal class DocumentQueryHandlers
    {
        private readonly Core core;

        public DocumentQueryHandlers(Core core)
        {
            this.core = core;
        }

        internal KbQueryResult ExecuteSelect(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbQueryResult();

                using (var transaction = core.Transactions.Acquire(processId))
                {
                    result = StaticSearcherMethods.FindDocumentsByPreparedQuery(core, transaction, preparedQuery);
                    transaction.Commit();
                    result.RowCount = result.Rows.Count;
                    result.Metrics = transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
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
                var result = new KbQueryResult();

                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var physicalDocument = new PhysicalDocument();

                    var keyValuePairs = preparedQuery.UpsertKeyValuePairs.ToDictionary(o => o.Field.Field, o => o.Value.Value);
                    physicalDocument.Content = JsonConvert.SerializeObject(keyValuePairs);
                    var physicalSchema = core.Schemas.Acquire(transaction, preparedQuery.Schemas.Single().Name, LockOperation.Write);
                    core.Documents.InsertDocument(transaction, physicalSchema, physicalDocument);

                    transaction.Commit();
                    result.RowCount = 1;
                    result.Metrics = transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteSample(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbQueryResult();

                using (var transaction = core.Transactions.Acquire(processId))
                {
                    string schemaName = preparedQuery.Schemas.Single().Name;
                    result = StaticSearcherMethods.SampleSchemaDocuments(core, transaction, schemaName, preparedQuery.RowLimit);
                    transaction.Commit();
                    result.RowCount = result.Rows.Count;
                    result.Metrics = transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteList(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbQueryResult();

                using (var transaction = core.Transactions.Acquire(processId))
                {
                    string schemaName = preparedQuery.Schemas.Single().Name;
                    result = StaticSearcherMethods.ListSchemaDocuments(core, transaction, schemaName, preparedQuery.RowLimit);
                    transaction.Commit();
                    result.RowCount = result.Rows.Count;
                    result.Metrics = transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteExplain(ulong processId, PreparedQuery preparedQuery)
        {
            throw new KbNotImplementedException();
            /*
            try
            {
                var result = new KbQueryResult();

                using (var transaction = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(transaction, preparedQuery.Schemas[0].n, LockOperation.Read);

                    var lookupOptimization = ConditionLookupOptimization.Build(core, transaction, physicalSchema, preparedQuery.Conditions);
                    result.Explanation = lookupOptimization.BuildFullVirtualExpression();

                    transaction.Commit();
                    result.RowCount = 0;
                    result.Metrics = transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
            */
        }

        internal KbActionResponse ExecuteDelete(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbActionResponse();

                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var firstSchema = preparedQuery.Schemas.First();

                    var physicalSchema = core.Schemas.Acquire(transaction, firstSchema.Name, LockOperation.Read);


                    string? getDocumentPointsForSchemaPrefix = firstSchema.Prefix;

                    if (preparedQuery.Attributes.ContainsKey(PreparedQuery.QueryAttribute.SpecificSchemaPrefix))
                    {
                        getDocumentPointsForSchemaPrefix = preparedQuery.Attributes[PreparedQuery.QueryAttribute.SpecificSchemaPrefix] as string;
                    }

                    Utility.EnsureNotNull(getDocumentPointsForSchemaPrefix);

                    var documentPointers = StaticSearcherMethods.FindDocumentPointersByPreparedQuery(core, transaction, preparedQuery, getDocumentPointsForSchemaPrefix);

                    core.Documents.DeleteDocuments(transaction, physicalSchema, documentPointers.ToArray());

                    transaction.Commit();
                    result.RowCount = documentPointers.Count();
                    result.Metrics = transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteDelete for process {processId}.", ex);
                throw;
            }
        }
    }
}
