using Katzebase.Engine.Query.Searchers;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Documents.Management
{
    public class DocumentAPIHandlers
    {
        private readonly Core core;

        public DocumentAPIHandlers(Core core)
        {
            this.core = core;
        }

        public KbQueryResult DocumentSample(ulong processId, string schemaName, int rowLimit)
        {
            try
            {
                var result = new KbQueryResult();

                using (var transaction = core.Transactions.Begin(processId))
                {
                    result = StaticSearcherMethods.SampleSchemaDocuments(core, transaction, schemaName, rowLimit);
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
        /// Returns all doucments in a schema with there values.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schemaName"></param>
        /// <param name="rowLimit"></param>
        /// <returns></returns>
        public KbQueryResult ListDocuments(ulong processId, string schemaName, int rowLimit = -1)
        {
            try
            {
                var result = new KbQueryResult();

                using (var transaction = core.Transactions.Begin(processId))
                {
                    result = StaticSearcherMethods.ListSchemaDocuments(core, transaction, schemaName, rowLimit);
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
        /// Saves a new document, this is used for inserts.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        /// <param name="newId"></param>
        /// <exception cref="KbObjectNotFoundException"></exception>
        public KbActionResponse StoreDocument(ulong processId, string schema, KbDocument document)
        {
            try
            {
                var result = new KbActionResponse();

                var physicalDocument = PhysicalDocument.FromClientPayload(document);
                physicalDocument.Created = DateTime.UtcNow;
                physicalDocument.Modfied = DateTime.UtcNow;

                using (var transaction = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(transaction, schema, LockOperation.Write);
                    core.Documents.InsertDocument(transaction, physicalSchema, physicalDocument);
                    transaction.Commit();
                    result.RowCount = 1;
                    result.Metrics = transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to store document for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns a list of all documents in a schema. Just the IDs, no values.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        /// <exception cref="KbObjectNotFoundException"></exception>
        public KbDocumentCatalogCollection DocumentCatalog(ulong processId, string schemaName)
        {
            try
            {
                using (var transaction = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
                    var documentPointers = core.Documents.AcquireDocumentPointers(transaction, physicalSchema, LockOperation.Read).ToList();

                    var result = new KbDocumentCatalogCollection();
                    foreach (var documentPointer in documentPointers)
                    {
                        result.Add(new KbDocumentCatalogItem() { Id = documentPointer.DocumentId });
                    }
                    transaction.Commit();
                    result.RowCount = documentPointers.Count;
                    result.Metrics = transaction.PT?.ToCollection();
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to get catalog for process {processId}.", ex);
                throw;
            }
        }


        /// <summary>
        /// Deletes a document by its ID.
        /// </summary>
        public KbActionResponse DeleteDocumentById(ulong processId, string schemaName, uint documentId)
        {
            try
            {
                var result = new KbActionResponse();

                using (var transaction = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
                    var documentPointers = core.Documents.AcquireDocumentPointers(transaction, physicalSchema, LockOperation.Read).ToList();

                    var pointersToDelete = documentPointers.Where(o => o.DocumentId == documentId);

                    core.Documents.DeleteDocuments(transaction, physicalSchema, pointersToDelete);

                    transaction.Commit();
                    result.RowCount = documentPointers.Count;
                    result.Metrics = transaction.PT?.ToCollection();
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete document by ID for process {processId}.", ex);
                throw;
            }
        }

    }
}
