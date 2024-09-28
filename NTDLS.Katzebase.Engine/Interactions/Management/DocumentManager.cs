using Newtonsoft.Json;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Engine.Schemas;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to documents.
    /// </summary>
    public class DocumentManager<TData> where TData : IStringable
    {
        private readonly EngineCore<TData> _core;

        internal DocumentQueryHandlers<TData> QueryHandlers { get; private set; }
        public DocumentAPIHandlers<TData> APIHandlers { get; private set; }

        internal DocumentManager(EngineCore<TData> core)
        {
            _core = core;

            try
            {
                QueryHandlers = new DocumentQueryHandlers<TData>(core);
                APIHandlers = new DocumentAPIHandlers<TData>(core);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate document manager.", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to read a document we do it here. If we already have the page number, we can take a shortcut.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documentId"></param>
        internal PhysicalDocument<TData> AcquireDocument(
            Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema, DocumentPointer<TData> documentPointer, LockOperation lockIntention)
        {
            try
            {
                var physicalDocumentPage = AcquireDocumentPage(transaction, physicalSchema, documentPointer.PageNumber, lockIntention);
                return physicalDocumentPage.Documents.First(o => o.Key == documentPointer.DocumentId).Value;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to acquire document for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal PhysicalDocumentPage<TData> AcquireDocumentPage(
            Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema, int pageNumber, LockOperation lockIntention)
        {
            try
            {
                return _core.IO.GetPBuf<PhysicalDocumentPage<TData>>(
                    transaction, physicalSchema.DocumentPageCatalogItemFilePath(pageNumber), lockIntention);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to acquire document page for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal IEnumerable<DocumentPointer<TData>> AcquireDocumentPointers(
            Transaction<TData> transaction, string schemaName, LockOperation lockIntention)
        {
            var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            return AcquireDocumentPointers(transaction, physicalSchema, lockIntention);
        }

        internal IEnumerable<DocumentPointer<TData>> AcquireDocumentPointers(
            Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema, LockOperation lockIntention, int limit = -1)
        {
            try
            {
                var physicalDocumentPageCatalog = _core.IO.GetPBuf<PhysicalDocumentPageCatalog>(
                    transaction, physicalSchema.DocumentPageCatalogFilePath(), lockIntention);

                var documentPointers = new List<DocumentPointer<TData>>();

                foreach (var item in physicalDocumentPageCatalog.Catalog)
                {
                    var physicalDocumentPageMap = AcquireDocumentPageMap(transaction, physicalSchema, item.PageNumber, lockIntention);
                    documentPointers.AddRange(physicalDocumentPageMap.DocumentIDs.Select(o => new DocumentPointer<TData>(item.PageNumber, o)));

                    if (limit > 0 && documentPointers.Count > limit)
                    {
                        break;
                    }
                }

                return documentPointers;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to acquire document pointers for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal PhysicalDocumentPageMap AcquireDocumentPageMap(
            Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema, int pageNumber, LockOperation lockIntention)
        {
            try
            {
                return _core.IO.GetPBuf<PhysicalDocumentPageMap>(transaction, physicalSchema.PhysicalDocumentPageMapFilePath(pageNumber), lockIntention);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to acquire document page map for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal PhysicalDocumentPageCatalog AcquireDocumentPageCatalog(
            Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema, LockOperation lockIntention)
        {
            try
            {
                return _core.IO.GetPBuf<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogFilePath(), lockIntention);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to acquire document page catalog for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        internal DocumentPointer<TData> InsertDocument(Transaction<TData> transaction, string schemaName, object pageContent)
        {
            var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            return InsertDocument(transaction, physicalSchema, JsonConvert.SerializeObject(pageContent));
        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        internal DocumentPointer<TData> InsertDocument(Transaction<TData> transaction, string schemaName, string pageContent)
        {
            var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            return InsertDocument(transaction, physicalSchema, pageContent);
        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        internal DocumentPointer<TData> InsertDocument(Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema, string pageContent)
        {
            try
            {
                //Open the document page catalog:
                var documentPageCatalog = _core.IO.GetPBuf<PhysicalDocumentPageCatalog>(
                    transaction, physicalSchema.DocumentPageCatalogFilePath(), LockOperation.Write);
                uint physicalDocumentId = documentPageCatalog.ConsumeNextDocumentId();

                var physicalDocument = new PhysicalDocument<TData>(pageContent)
                {
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow,
                };

                PhysicalDocumentPageMap physicalDocumentPageMap;
                PhysicalDocumentPage<TData> documentPage;

                //Find a page with some empty room:
                var existingPhysicalPageCatalogItem = documentPageCatalog.GetPageWithRoomForNewDocument(physicalSchema.PageSize);

                PhysicalDocumentPageCatalogItem physicalPageCatalogItem;

                if (existingPhysicalPageCatalogItem == null)
                {
                    //We didn't find a page with room, we're going to have to create a new "Page Catalog Item" and new "Document Page Map".
                    // add the given document ID to it and add that catalog item to the catalog collection:
                    physicalPageCatalogItem = new PhysicalDocumentPageCatalogItem(documentPageCatalog.NextPageNumber())
                    {
                        DocumentCount = 1
                    };

                    //Create a new document page map.
                    physicalDocumentPageMap = new PhysicalDocumentPageMap();
                    //Insert into thr page map.
                    physicalDocumentPageMap.DocumentIDs.Add(physicalDocumentId);

                    //We created a new page item, add it to the catalog:
                    documentPageCatalog.Catalog.Add(physicalPageCatalogItem);

                    //Create the new page, this will store the actual document contents.
                    documentPage = new PhysicalDocumentPage<TData>();

                    //Add the given document to the page document.
                    documentPage.Documents.Add(physicalDocumentId, physicalDocument);
                }
                else
                {
                    physicalPageCatalogItem = existingPhysicalPageCatalogItem;

                    physicalPageCatalogItem.DocumentCount++;

                    //Open the page and add the document to it.
                    documentPage = AcquireDocumentPage(transaction, physicalSchema, physicalPageCatalogItem.PageNumber, LockOperation.Write);

                    //Add the given document to the page document.
                    documentPage.Documents.Add(physicalDocumentId, physicalDocument);

                    //Get the document page map.
                    physicalDocumentPageMap = AcquireDocumentPageMap(
                        transaction, physicalSchema, physicalPageCatalogItem.PageNumber, LockOperation.Write);

                    //Insert into the page map.
                    physicalDocumentPageMap.DocumentIDs.Add(physicalDocumentId);
                }

                //Save the document page map.
                _core.IO.PutPBuf(transaction, physicalSchema.PhysicalDocumentPageMapFilePath(
                    physicalPageCatalogItem.PageNumber), physicalDocumentPageMap);

                //Save the document page:
                _core.IO.PutPBuf(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(physicalPageCatalogItem), documentPage);

                //Save the document page catalog:
                _core.IO.PutPBuf(transaction, physicalSchema.DocumentPageCatalogFilePath(), documentPageCatalog);

                var documentPointer = new DocumentPointer<TData>(physicalPageCatalogItem.PageNumber, physicalDocumentId);

                //Update all of the indexes that reference the document.
                _core.Indexes.InsertDocumentIntoIndexes(transaction, physicalSchema, physicalDocument, documentPointer);

                return documentPointer;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to insert document for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to update multiple documents in the same schema, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documents">List of document pointers and their new content.</param>
        /// <param name="listOfModifiedFields">A list of the fields that were modified so that we
        /// can filter the indexes we need to update.</param>
        internal void UpdateDocuments(Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema,
            List<DocumentPointer<TData>> updatedDocumentPointers, IEnumerable<string>? listOfModifiedFields = null)
        {
            try
            {
                var physicalDocuments = new Dictionary<DocumentPointer<TData>, PhysicalDocument<TData>>();

                foreach (var documentPointer in updatedDocumentPointers)
                {
                    var documentPage = AcquireDocumentPage(transaction, physicalSchema, documentPointer.PageNumber, LockOperation.Write);

                    var physicalDocument = documentPage.Documents[documentPointer.DocumentId];
                    physicalDocument.Modified = DateTime.UtcNow;
                    documentPage.Documents[documentPointer.DocumentId] = physicalDocument;

                    //Save the document page:
                    _core.IO.PutPBuf(transaction, physicalSchema.DocumentPageCatalogItemFilePath(documentPointer), documentPage);

                    physicalDocuments.Add(documentPointer, physicalDocument);
                }

                //Update all of the indexes that reference the document.
                _core.Indexes.UpdateDocumentsIntoIndexes(transaction, physicalSchema, physicalDocuments, listOfModifiedFields);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to update document for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to update a document, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void DeleteDocuments(Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema, IEnumerable<DocumentPointer<TData>> documentPointers)
        {
            try
            {
                //Open the document page catalog:
                var documentPageCatalog = _core.IO.GetPBuf<PhysicalDocumentPageCatalog>(
                    transaction, physicalSchema.DocumentPageCatalogFilePath(), LockOperation.Write);

                foreach (var documentPointer in documentPointers)
                {
                    var documentPage = AcquireDocumentPage(transaction, physicalSchema, documentPointer.PageNumber, LockOperation.Write);

                    var CopyOfDocumentPageCatalog = documentPageCatalog.Catalog[documentPointer.PageNumber];

                    CopyOfDocumentPageCatalog.DocumentCount--;

                    documentPageCatalog.Catalog[documentPointer.PageNumber] = CopyOfDocumentPageCatalog;

                    //Remove the item from the document page.
                    documentPage.Documents.Remove(documentPointer.DocumentId);

                    //Save the document page:
                    _core.IO.PutPBuf(transaction, physicalSchema.DocumentPageCatalogItemFilePath(documentPointer), documentPage);

                    //Update the document page map.
                    var physicalDocumentPageMap = AcquireDocumentPageMap(
                        transaction, physicalSchema, documentPointer.PageNumber, LockOperation.Write);
                    physicalDocumentPageMap.DocumentIDs.Remove(documentPointer.DocumentId);

                    _core.IO.PutPBuf(transaction, physicalSchema.PhysicalDocumentPageMapFilePath(
                        documentPointer.PageNumber), physicalDocumentPageMap);
                }

                //Save the document page catalog:
                _core.IO.PutPBuf(transaction, physicalSchema.DocumentPageCatalogFilePath(), documentPageCatalog);

                if (documentPointers.Any())
                {
                    //Update all of the indexes that reference the documents.
                    _core.Indexes.RemoveDocumentsFromIndexes(transaction, physicalSchema, documentPointers);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to delete documents for process {transaction.ProcessId}.", ex);
                throw;
            }
        }
    }
}
