﻿using Newtonsoft.Json;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.PersistentTypes.Schema;
using System.Diagnostics;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to documents.
    /// </summary>
    public class DocumentManager
    {
        private readonly EngineCore _core;

        internal DocumentQueryHandlers QueryHandlers { get; private set; }
        public DocumentAPIHandlers APIHandlers { get; private set; }

        internal DocumentManager(EngineCore core)
        {
            _core = core;

            try
            {
                QueryHandlers = new DocumentQueryHandlers(core);
                APIHandlers = new DocumentAPIHandlers(core);
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
        internal PhysicalDocument AcquireDocument(
            Transaction transaction, PhysicalSchema physicalSchema, DocumentPointer documentPointer, LockOperation lockIntention)
        {
            try
            {
                var physicalDocumentPage = AcquireDocumentPage(transaction, physicalSchema, documentPointer.PageNumber, lockIntention);
                return physicalDocumentPage.Documents.First(o => o.Key == documentPointer.DocumentId).Value;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal PhysicalDocumentPage AcquireDocumentPage(
            Transaction transaction, PhysicalSchema physicalSchema, int pageNumber, LockOperation lockIntention)
        {
            try
            {
                return _core.IO.GetPBuf<PhysicalDocumentPage>(
                    transaction, physicalSchema.DocumentPageCatalogItemFilePath(pageNumber), lockIntention);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal IEnumerable<DocumentPointer> AcquireDocumentPointers(
            Transaction transaction, string schemaName, LockOperation lockIntention)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                return AcquireDocumentPointers(transaction, physicalSchema, lockIntention);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal IEnumerable<DocumentPointer> AcquireDocumentPointers(
            Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockIntention, int limit = -1)
        {
            try
            {
                var physicalDocumentPageCatalog = _core.IO.GetPBuf<PhysicalDocumentPageCatalog>(
                    transaction, physicalSchema.DocumentPageCatalogFilePath(), lockIntention);

                var documentPointers = new List<DocumentPointer>();

                foreach (var item in physicalDocumentPageCatalog.Catalog)
                {
                    var physicalDocumentPageMap = AcquireDocumentPageMap(transaction, physicalSchema, item.PageNumber, lockIntention);
                    documentPointers.AddRange(physicalDocumentPageMap.DocumentIDs.Select(o => new DocumentPointer(item.PageNumber, o)));

                    if (limit > 0 && documentPointers.Count > limit)
                    {
                        break;
                    }
                }

                return documentPointers;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal PhysicalDocumentPageMap AcquireDocumentPageMap(
            Transaction transaction, PhysicalSchema physicalSchema, int pageNumber, LockOperation lockIntention)
        {
            try
            {
                return _core.IO.GetPBuf<PhysicalDocumentPageMap>(transaction, physicalSchema.PhysicalDocumentPageMapFilePath(pageNumber), lockIntention);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal PhysicalDocumentPageCatalog AcquireDocumentPageCatalog(
            Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockIntention)
        {
            try
            {
                return _core.IO.GetPBuf<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogFilePath(), lockIntention);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        internal DocumentPointer InsertDocument(Transaction transaction, string schemaName, object pageContent)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                return InsertDocument(transaction, physicalSchema, JsonConvert.SerializeObject(pageContent));
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        internal DocumentPointer InsertDocument(Transaction transaction, string schemaName, string pageContent)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                return InsertDocument(transaction, physicalSchema, pageContent);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        internal DocumentPointer InsertDocument(Transaction transaction, PhysicalSchema physicalSchema, string pageContent)
        {
            try
            {
                //Open the document page catalog:
                var documentPageCatalog = _core.IO.GetPBuf<PhysicalDocumentPageCatalog>(
                    transaction, physicalSchema.DocumentPageCatalogFilePath(), LockOperation.Write);
                uint physicalDocumentId = documentPageCatalog.ConsumeNextDocumentId();

                var physicalDocument = new PhysicalDocument(pageContent)
                {
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow,
                };

                PhysicalDocumentPageMap physicalDocumentPageMap;
                PhysicalDocumentPage documentPage;

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
                    documentPage = new PhysicalDocumentPage();

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

                var documentPointer = new DocumentPointer(physicalPageCatalogItem.PageNumber, physicalDocumentId);

                //Update all of the indexes that reference the document.
                _core.Indexes.InsertDocumentIntoIndexes(transaction, physicalSchema, physicalDocument, documentPointer);

                return documentPointer;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to update multiple documents in the same schema, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="updatedDocuments">List of document pointers and their new content.</param>
        internal void UpdateDocuments(Transaction transaction, PhysicalSchema physicalSchema,
            Dictionary<DocumentPointer, KbInsensitiveDictionary<string?>> updatedDocuments)
        {
            try
            {
                var indexingDocuments = new Dictionary<DocumentPointer, PhysicalDocument>();
                var modifiedFieldNames = new HashSet<string>();

                foreach (var updatedDocument in updatedDocuments)
                {
                    //Open the page:
                    var physicalDocumentPage = AcquireDocumentPage(transaction, physicalSchema, updatedDocument.Key.PageNumber, LockOperation.Write);

                    //Get the document:
                    var physicalDocument = physicalDocumentPage.Documents[updatedDocument.Key.DocumentId];

                    physicalDocument.Modified = DateTime.UtcNow;

                    //Update all of the modified values into the document:
                    foreach (var updatedValue in updatedDocument.Value)
                    {
                        physicalDocument.Elements[updatedValue.Key] = updatedValue.Value;
                        modifiedFieldNames.Add(updatedValue.Key);
                    }

                    //Keep track of the modified physical documents for indexing:
                    indexingDocuments.Add(updatedDocument.Key, physicalDocument);

                    //Save the document page:
                    _core.IO.PutPBuf(transaction, physicalSchema.DocumentPageCatalogItemFilePath(updatedDocument.Key), physicalDocumentPage);
                }

                //var modifiedFieldNames = documentContent.Select(o=>o.Value);

                //Update all of the indexes that reference the document.
                _core.Indexes.UpdateDocumentsIntoIndexes(transaction, physicalSchema, indexingDocuments, modifiedFieldNames);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }
    }
}
