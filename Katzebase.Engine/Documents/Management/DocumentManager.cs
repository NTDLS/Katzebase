using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Schemas;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Documents.Management
{
    /// <summary>
    /// Provides core document management functionality. Reading, writing, locking, listing, etc.
    /// </summary>
    public class DocumentManager
    {
        private Core core;
        internal DocumentQueryHandlers QueryHandlers { get; set; }
        public DocumentAPIHandlers APIHandlers { get; set; }

        public DocumentManager(Core core)
        {
            this.core = core;
            QueryHandlers = new DocumentQueryHandlers(core);
            APIHandlers = new DocumentAPIHandlers(core);
        }

        /// <summary>
        /// When we want to read a document we do it here - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documentId"></param>
        internal PhysicalDocument AcquireDocument(Transaction transaction, PhysicalSchema physicalSchema, uint documentId, LockOperation lockIntention)
        {
            var documentPageCatalog = core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogDiskPath(), lockIntention);

            //Get the page that the document current exists in if any.
            var physicalPageMap = documentPageCatalog.GetDocumentPageMap(documentId);
            Utility.EnsureNotNull(physicalPageMap);

            //We found a page that contains the document, we need to open the page and modify the document with the given document id.
            var physicalDocumentPage = core.IO.GetJson<PhysicalDocumentPage>(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(physicalPageMap), lockIntention);

            return physicalDocumentPage.Documents.First(o => o.Key == documentId).Value;
        }

        /// <summary>
        /// When we want to read a document we do it here. If we already have the page number, we can take a shortcut.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documentId"></param>
        internal PhysicalDocument AcquireDocument(Transaction transaction, PhysicalSchema physicalSchema, DocumentPointer documentPointer, LockOperation lockIntention)
        {
            var physicalDocumentPage = core.IO.GetJson<PhysicalDocumentPage>(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(documentPointer), lockIntention);
            return physicalDocumentPage.Documents.First(o => o.Key == documentPointer.DocumentId).Value;
        }

        internal IEnumerable<DocumentPointer> AcquireDocumentPointers(Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockIntention)
        {
            var physicalDocumentPageCatalog = core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogDiskPath(), lockIntention);
            return physicalDocumentPageCatalog.ConsolidatedDocumentPointers();
        }

        internal PhysicalDocumentPageCatalog AcquireDocumentPageCatalog(Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockIntention)
        {
            return core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogDiskPath(), lockIntention);
        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void InsertDocument(Transaction transaction, PhysicalSchema physicalSchema, PhysicalDocument physicalDocument)
        {
            PhysicalDocumentPage documentPage;

            //Open the document page catalog:
            var documentPageCatalog = core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogDiskPath(), LockOperation.Write);
            Utility.EnsureNotNull(documentPageCatalog);

            physicalDocument.Id = documentPageCatalog.ConsumeNextDocumentId();

            //Find a page with some empty room:
            var physicalPageMap = documentPageCatalog.GetPageWithRoomForNewDocument(core.Settings.DocumentPageSize);

            if (physicalPageMap == null)
            {
                //Still didnt find a page with room, we're going to have to create a new "page catalog item",
                // add the given document ID to it and add that catalog item to the catalog collection:
                physicalPageMap = new PhysicalDocumentPageMap(documentPageCatalog.NextPageNumber());
                physicalPageMap.DocumentIDs.Add(physicalDocument.Id);

                //We created a new page item, add it to the catalog:
                documentPageCatalog.PageMappings.Add(physicalPageMap);

                //Create the new page, this will store the actual document contents.
                documentPage = new PhysicalDocumentPage(physicalPageMap.PageNumber);

                //Add the given document to the page document.
                documentPage.Documents.Add(physicalDocument.Id, physicalDocument);
            }
            else
            {
                //We found a page with space, just add the document ID to the page catalog item.
                physicalPageMap.DocumentIDs.Add(physicalDocument.Id);

                //Open the page and add the document to it.
                documentPage = core.IO.GetJson<PhysicalDocumentPage>(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(physicalPageMap), LockOperation.Write);

                //Add the given document to the page document.
                documentPage.Documents.Add(physicalDocument.Id, physicalDocument);
            }


            //Save the document page:
            core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(physicalPageMap), documentPage);

            //Save the docuemnt page catalog:
            core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogDiskPath(), documentPageCatalog);

            //Update all of the indexes that referecne the document.
            core.Indexes.InsertDocumentIntoIndexes(transaction, physicalSchema, physicalDocument, new DocumentPointer(documentPage.PageNumber, physicalDocument.Id));
        }

        /// <summary>
        /// When we want to update a document, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void UpdateDocument(Transaction transaction, PhysicalSchema physicalSchema, uint documentId, string content)
        {
            //Open the document page catalog:
            var documentPageCatalog = core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogDiskPath(), LockOperation.Write);
            Utility.EnsureNotNull(documentPageCatalog);

            //Get the page that the document current exists in, if any.
            var physicalPageMap = documentPageCatalog.GetDocumentPageMap(documentId);
            if (physicalPageMap == null)
            {
                throw new KbObjectNotFoundException($"The document with the ID [{documentId}] was not found.");
            }

            //We found a page that contains the document, we need to open the page and modify the document with the given document id.
            var documentPage = core.IO.GetJson<PhysicalDocumentPage>(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(physicalPageMap), LockOperation.Write);

            var newPhysicalDocument = new PhysicalDocument()
            {
                Id = documentId,
                Content = content,
                Created = documentPage.Documents[documentId].Created,
                Modfied = DateTime.UtcNow
            };

            documentPage.Documents[documentId] = newPhysicalDocument;

            //Save the document page:
            core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(physicalPageMap), documentPage);

            //Save the docuemnt page catalog:
            core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogDiskPath(), documentPageCatalog);

            //Update all of the indexes that referecne the document.
            core.Indexes.InsertDocumentIntoIndexes(transaction, physicalSchema, newPhysicalDocument, new DocumentPointer(documentPage.PageNumber, newPhysicalDocument.Id));
        }

        /// <summary>
        /// When we want to update a document, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void DeleteDocuments(Transaction transaction, PhysicalSchema physicalSchema, IEnumerable<DocumentPointer> documentPointers)
        {
            //Open the document page catalog:
            var documentPageCatalog = core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogDiskPath(), LockOperation.Write);
            Utility.EnsureNotNull(documentPageCatalog);

            foreach (var documentPointer in documentPointers)
            {
                var documentPage = core.IO.GetJson<PhysicalDocumentPage>(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(documentPointer), LockOperation.Write);

                //Remove the item from the document page.
                documentPage.Documents.Remove(documentPointer.DocumentId);

                //Save the document page:
                core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(documentPointer), documentPage);

                documentPageCatalog.PageMappings[documentPointer.PageNumber].DocumentIDs.Remove(documentPointer.DocumentId);

                //Save the docuemnt page catalog:
                core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogDiskPath(), documentPageCatalog);
            }

            //Update all of the indexes that referecne the documents.
            core.Indexes.DeleteDocumentsFromIndexes(transaction, physicalSchema, documentPointers);
        }
    }
}
