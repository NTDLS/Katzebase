using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Query.Searchers;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to documents.
    /// </summary>
    public class DocumentAPIHandlers
    {
        private readonly Core _core;

        public DocumentAPIHandlers(Core core)
        {
            _core = core;

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
                using var transactionReference = _core.Transactions.Acquire(processId);
                var result = StaticSearcherMethods.SampleSchemaDocuments(_core, transactionReference.Transaction, schemaName, rowLimit);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute document sample for process id {processId}.", ex);
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
                using var transactionReference = _core.Transactions.Acquire(processId);
                var result = StaticSearcherMethods.ListSchemaDocuments(_core, transactionReference.Transaction, schemaName, rowLimit);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, result.Rows.Count);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute document list for process id {processId}.", ex);
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
                using var transactionReference = _core.Transactions.Acquire(processId);
                var result = new KbActionResponseUInt()
                {
                    Value = _core.Documents.InsertDocument(transactionReference.Transaction, schemaName, document.Content).DocumentId,
                };

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, 1);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute document store for process id {processId}.", ex);
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
                using var transactionReference = _core.Transactions.Acquire(processId);
                var result = new KbDocumentCatalogCollection();
                var documentPointers = _core.Documents.AcquireDocumentPointers(transactionReference.Transaction, schemaName, LockOperation.Read).ToList();

                result.Collection.AddRange(documentPointers.Select(o => new KbDocumentCatalogItem(o.DocumentId)));
                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, documentPointers.Count);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute document catalog for process id {processId}.", ex);
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
                using var transactionReference = _core.Transactions.Acquire(processId);
                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, schemaName, LockOperation.Write);
                var documentPointers = _core.Documents.AcquireDocumentPointers(transactionReference.Transaction, physicalSchema, LockOperation.Write).ToList();
                var pointersToDelete = documentPointers.Where(o => o.DocumentId == documentId);

                _core.Documents.DeleteDocuments(transactionReference.Transaction, physicalSchema, pointersToDelete);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(documentPointers.Count);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute document delete for process id {processId}.", ex);
                throw;
            }
        }
    }
}
