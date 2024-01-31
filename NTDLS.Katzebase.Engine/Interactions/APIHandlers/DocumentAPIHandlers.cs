using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Client.Payloads.RoundTrip;
using NTDLS.Katzebase.Engine.Query.Searchers;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to documents.
    /// </summary>
    public class DocumentAPIHandlers
    {
        private readonly EngineCore _core;

        public DocumentAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instantiate document API handlers.", ex);
                throw;
            }
        }

        public KbQueryDocumentSampleReply DocumentSample(ulong processId, KbQueryDocumentSample param)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var result = (KbQueryDocumentSampleReply)StaticSearcherMethods.SampleSchemaDocuments(_core, transactionReference.Transaction, param.Schema, param.Count);
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
        public KbQueryDocumentListReply ListDocuments(ulong processId, KbQueryDocumentList param)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var nativeResults = StaticSearcherMethods.ListSchemaDocuments(_core, transactionReference.Transaction, param.Schema, param.Count);

                var apiResults = new KbQueryDocumentListReply()
                {
                    Rows = nativeResults.Rows,
                    Fields = nativeResults.Fields
                };

                return transactionReference.CommitAndApplyMetricsThenReturnResults(apiResults, apiResults.Rows.Count);
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
        public KbQueryDocumentStoreReply StoreDocument(ulong processId, KbQueryDocumentStore param)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var result = new KbQueryDocumentStoreReply()
                {
                    Value = _core.Documents.InsertDocument(transactionReference.Transaction, param.Schema, param.Document.Content).DocumentId,
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
        public KbQueryDocumentCatalogReply DocumentCatalog(ulong processId, KbQueryDocumentCatalog param)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var result = new KbQueryDocumentCatalogReply();
                var documentPointers = _core.Documents.AcquireDocumentPointers(transactionReference.Transaction, param.Schema, LockOperation.Read).ToList();

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
        public KbQueryDocumentDeleteByIdReply DeleteDocumentById(ulong processId, KbQueryDocumentDeleteById param)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var physicalSchema = _core.Schemas.Acquire(transactionReference.Transaction, param.Schema, LockOperation.Write);
                var documentPointers = _core.Documents.AcquireDocumentPointers(transactionReference.Transaction, physicalSchema, LockOperation.Write).ToList();
                var pointersToDelete = documentPointers.Where(o => o.DocumentId == param.Id);

                _core.Documents.DeleteDocuments(transactionReference.Transaction, physicalSchema, pointersToDelete);

                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryDocumentDeleteByIdReply(), documentPointers.Count);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute document delete for process id {processId}.", ex);
                throw;
            }
        }
    }
}
