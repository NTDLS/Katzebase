using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Schemas;
using Katzebase.PublicLibrary;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Documents.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to documents.
    /// </summary>
    public class DocumentManager
    {
        private readonly Core core;
        internal DocumentQueryHandlers QueryHandlers { get; set; }
        public DocumentAPIHandlers APIHandlers { get; set; }

        public DocumentManager(Core core)
        {
            this.core = core;

            try
            {
                QueryHandlers = new DocumentQueryHandlers(core);
                APIHandlers = new DocumentAPIHandlers(core);
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate document manager.", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to read a document we do it here. If we already have the page number, we can take a shortcut.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documentId"></param>
        internal PhysicalDocument AcquireDocument(Transaction transaction, PhysicalSchema physicalSchema, DocumentPointer documentPointer, LockOperation lockIntention)
        {
            try
            {
                var physicalDocumentPage = AcquireDocumentPage(transaction, physicalSchema, documentPointer.PageNumber, lockIntention);
                return physicalDocumentPage.Documents.First(o => o.Key == documentPointer.DocumentId).Value;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to acquire document for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal PhysicalDocumentPage AcquireDocumentPage(Transaction transaction, PhysicalSchema physicalSchema, int pageNumber, LockOperation lockIntention)
        {
            try
            {
                return core.IO.GetJson<PhysicalDocumentPage>(transaction, physicalSchema.DocumentPageCatalogItemFilePath(pageNumber), lockIntention);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to acquire document page for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal IEnumerable<DocumentPointer> AcquireDocumentPointers(Transaction transaction, string schemaName, LockOperation lockIntention)
        {
            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            return AcquireDocumentPointers(transaction, physicalSchema, lockIntention);
        }

        internal IEnumerable<DocumentPointer> AcquireDocumentPointers(Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockIntention)
        {
            try
            {
                var physicalDocumentPageCatalog = core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogFilePath(), lockIntention);

                var documentPointers = new List<DocumentPointer>();

                foreach (var item in physicalDocumentPageCatalog.Catalog)
                {
                    var physicalDocumentPageMap = AcquireDocumentPageMap(transaction, physicalSchema, item.PageNumber, lockIntention);
                    documentPointers.AddRange(physicalDocumentPageMap.DocumentIDs.Select(o => new DocumentPointer(item.PageNumber, o)));
                }

                return documentPointers;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to acquire document pointers for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal PhysicalDocumentPageMap AcquireDocumentPageMap(Transaction transaction, PhysicalSchema physicalSchema, int pageNumber, LockOperation lockIntention)
        {
            try
            {
                return core.IO.GetJson<PhysicalDocumentPageMap>(transaction, physicalSchema.PhysicalDocumentPageMapFilePath(pageNumber), lockIntention);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to acquire document page map for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal PhysicalDocumentPageCatalog AcquireDocumentPageCatalog(Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockIntention)
        {
            try
            {
                return core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogFilePath(), lockIntention);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to acquire document page catalog for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        internal DocumentPointer InsertDocument(Transaction transaction, string schemaName, string pageContent)
        {
            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            return InsertDocument(transaction, physicalSchema, pageContent);

        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        internal DocumentPointer InsertDocument(Transaction transaction, PhysicalSchema physicalSchema, string pageContent)
        {
            try
            {
                //Open the document page catalog:
                var documentPageCatalog = core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogFilePath(), LockOperation.Write);
                KbUtility.EnsureNotNull(documentPageCatalog);

                var physicalDocument = new PhysicalDocument
                {
                    Id = documentPageCatalog.ConsumeNextDocumentId(),
                    Content = pageContent,
                    Created = DateTime.UtcNow,
                    Modfied = DateTime.UtcNow,
                };

                PhysicalDocumentPageMap physicalDocumentPageMap;
                PhysicalDocumentPage documentPage;

                //Find a page with some empty room:
                var physicalPageCatalogItem = documentPageCatalog.GetPageWithRoomForNewDocument(core.Settings.DocumentPageSize);

                if (physicalPageCatalogItem == null)
                {
                    //We didnt find a page with room, we're going to have to create a new "Page Catalog Item" and new "Document Page Map".
                    // add the given document ID to it and add that catalog item to the catalog collection:
                    physicalPageCatalogItem = new PhysicalDocumentPageCatalogItem(documentPageCatalog.NextPageNumber());
                    physicalPageCatalogItem.DocumentCount = 1;

                    //Create a new document page map.
                    physicalDocumentPageMap = new PhysicalDocumentPageMap();
                    //Insert into thr page map.
                    physicalDocumentPageMap.DocumentIDs.Add(physicalDocument.Id);

                    //We created a new page item, add it to the catalog:
                    documentPageCatalog.Catalog.Add(physicalPageCatalogItem);

                    //Create the new page, this will store the actual document contents.
                    documentPage = new PhysicalDocumentPage(physicalPageCatalogItem.PageNumber);

                    //Add the given document to the page document.
                    documentPage.Documents.Add(physicalDocument.Id, physicalDocument);
                }
                else
                {
                    physicalPageCatalogItem.DocumentCount++;

                    //Open the page and add the document to it.
                    documentPage = AcquireDocumentPage(transaction, physicalSchema, physicalPageCatalogItem.PageNumber, LockOperation.Write);

                    //Add the given document to the page document.
                    documentPage.Documents.Add(physicalDocument.Id, physicalDocument);

                    //Get the document page map.
                    physicalDocumentPageMap = AcquireDocumentPageMap(transaction, physicalSchema, physicalPageCatalogItem.PageNumber, LockOperation.Write);

                    //Insert into the page map.
                    physicalDocumentPageMap.DocumentIDs.Add(physicalDocument.Id);
                }

                //Save the document page map.
                core.IO.PutJson(transaction, physicalSchema.PhysicalDocumentPageMapFilePath(physicalPageCatalogItem.PageNumber), physicalDocumentPageMap);

                //Save the document page:
                core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(physicalPageCatalogItem), documentPage);

                //Save the document page catalog:
                core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogFilePath(), documentPageCatalog);

                var documentPointer = new DocumentPointer(documentPage.PageNumber, physicalDocument.Id);

                //Update all of the indexes that referecne the document.
                core.Indexes.InsertDocumentIntoIndexes(transaction, physicalSchema, physicalDocument, documentPointer);

                return documentPointer;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to insert document for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to update multiple documents in the same schema, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documents">List of dovuemtn pointers and their new content.</param>
        /// <param name="listOfModifiedFields">A list of the fields that were modified so that we can filter the indexes we need to update.</param>
        internal void UpdateDocuments(Transaction transaction, PhysicalSchema physicalSchema,
            Dictionary<DocumentPointer, string> documents, IEnumerable<string>? listOfModifiedFields = null)
        {
            try
            {
                var physicalDocuments = new Dictionary<DocumentPointer, PhysicalDocument>();

                foreach (var document in documents)
                {
                    var documentPage = AcquireDocumentPage(transaction, physicalSchema, document.Key.PageNumber, LockOperation.Write);

                    var physicalDocument = documentPage.Documents[document.Key.DocumentId];
                    physicalDocument.Content = document.Value;
                    physicalDocument.Modfied = DateTime.UtcNow;
                    documentPage.Documents[document.Key.DocumentId] = physicalDocument;

                    //Save the document page:
                    core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogItemFilePath(document.Key), documentPage);

                    physicalDocuments.Add(document.Key, physicalDocument);
                }

                //Update all of the indexes that referecne the document.
                core.Indexes.UpdateDocumentsIntoIndexes(transaction, physicalSchema, physicalDocuments, listOfModifiedFields);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to update document for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to update a document, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void DeleteDocuments(Transaction transaction, PhysicalSchema physicalSchema, IEnumerable<DocumentPointer> documentPointers)
        {
            try
            {
                //Open the document page catalog:
                var documentPageCatalog = core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogFilePath(), LockOperation.Write);
                KbUtility.EnsureNotNull(documentPageCatalog);

                foreach (var documentPointer in documentPointers)
                {
                    var documentPage = AcquireDocumentPage(transaction, physicalSchema, documentPointer.PageNumber, LockOperation.Write);

                    documentPageCatalog.Catalog[documentPointer.PageNumber].DocumentCount--;

                    //Remove the item from the document page.
                    documentPage.Documents.Remove(documentPointer.DocumentId);

                    //Save the document page:
                    core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogItemFilePath(documentPointer), documentPage);

                    //Update the document page map.
                    var physicalDocumentPageMap = AcquireDocumentPageMap(transaction, physicalSchema, documentPointer.PageNumber, LockOperation.Write);
                    physicalDocumentPageMap.DocumentIDs.Remove(documentPointer.DocumentId);
                    core.IO.PutJson(transaction, physicalSchema.PhysicalDocumentPageMapFilePath(documentPointer.PageNumber), physicalDocumentPageMap);
                }

                //Save the document page catalog:
                core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogFilePath(), documentPageCatalog);

                if (documentPointers.Count() > 0)
                {
                    //Update all of the indexes that referecne the documents.
                    core.Indexes.RemoveDocumentsFromIndexes(transaction, physicalSchema, documentPointers);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete documents for process {transaction.ProcessId}.", ex);
                throw;
            }
        }
    }
}
