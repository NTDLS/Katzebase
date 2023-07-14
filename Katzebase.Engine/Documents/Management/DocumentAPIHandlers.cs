using Katzebase.Engine.Query.Searchers;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Documents.Management
{
    /// <summary>
    /// Public class methods for handling API requests related to documents.
    /// </summary>
    public class DocumentAPIHandlers
    {
        private readonly Core core;

        public DocumentAPIHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate document API handlers.", ex);
                throw;
            }
        }

        public KbQueryResult DocumentSample(ulong processId, string schemaName, int rowLimit)
        {
            try
            {
                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = StaticSearcherMethods.SampleSchemaDocuments(core, txRef.Transaction, schemaName, rowLimit);

                    txRef.Commit();
                    result.RowCount = result.Rows.Count;
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
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
                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = StaticSearcherMethods.ListSchemaDocuments(core, txRef.Transaction, schemaName, rowLimit);

                    txRef.Commit();
                    result.RowCount = result.Rows.Count;
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
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

        /// <summary>
        /// Saves a new document, this is used for inserts.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        /// <param name="newId"></param>
        /// <exception cref="KbObjectNotFoundException"></exception>
        public KbActionResponseUInt StoreDocument(ulong processId, string schemaName, KbDocument document)
        {
            try
            {
                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponseUInt()
                    {
                        Id = core.Documents.InsertDocument(txRef.Transaction, schemaName, document.Content).DocumentId,
                    };

                    txRef.Commit();
                    result.RowCount = 1;
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute document store for process id {processId}.", ex);
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
                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = new KbDocumentCatalogCollection();
                    var documentPointers = core.Documents.AcquireDocumentPointers(txRef.Transaction, schemaName, LockOperation.Read).ToList();
                    result.AddRange(documentPointers.Select(o => new KbDocumentCatalogItem(o.DocumentId)));

                    txRef.Commit();
                    result.RowCount = documentPointers.Count;
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute document catalog for process id {processId}.", ex);
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
                using (var txRef = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();
                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schemaName, LockOperation.Write);
                    var documentPointers = core.Documents.AcquireDocumentPointers(txRef.Transaction, physicalSchema, LockOperation.Write).ToList();
                    var pointersToDelete = documentPointers.Where(o => o.DocumentId == documentId);
                    core.Documents.DeleteDocuments(txRef.Transaction, physicalSchema, pointersToDelete);

                    txRef.Commit();
                    result.RowCount = documentPointers.Count;
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
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
