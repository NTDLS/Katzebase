using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Searchers;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is the class that all API controllers should interface with for document access.
    /// </summary>
    public class DocumentManager
    {
        private Core core;

        public DocumentManager(Core core)
        {
            this.core = core;
        }

        #region Query Handlers.

        internal KbQueryResult ExecuteSelect(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbQueryResult();

                using (var txRef = core.Transactions.Begin(processId))
                {
                    result = StaticSearcherMethods.FindDocumentsByPreparedQuery(core, txRef.Transaction, preparedQuery);
                    txRef.Commit();
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
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
            return ExecuteSample(processId, preparedQuery.Schemas.First().Name, preparedQuery.RowLimit);
        }

        internal KbQueryResult ExecuteList(ulong processId, PreparedQuery preparedQuery)
        {
            throw new KbNotImplementedException();
            //return ExecuteList(processId, preparedQuery.Schemas.First().Name, preparedQuery.RowLimit);
        }


        internal KbQueryResult ExecuteExplain(ulong processId, PreparedQuery preparedQuery)
        {
            throw new KbNotImplementedException();
            /*
            try
            {
                var result = new KbQueryResult();

                using (var txRef = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, preparedQuery.Schemas[0].Prefix, LockOperation.Read);

                    var lookupOptimization = ConditionLookupOptimization.Build(core, txRef.Transaction, physicalSchema, preparedQuery.Conditions);
                    result.Explanation = lookupOptimization.BuildFullVirtualExpression();

                    txRef.Commit();

                    result.Metrics = txRef.Transaction.PT?.ToCollection();
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
            throw new KbNotImplementedException();

            /*
            try
            {
                var result = new KbActionResponse();

                using (var txRef = core.Transactions.Begin(processId))
                {
                    result = StaticSearcherMethods.FindDocumentsByPreparedQuery(core, txRef.Transaction, preparedQuery);

                    //TODO: Delete the documents.

                    var documentCatalog = core.IO.GetJson<PhysicalDocumentCatalog>(txRef.Transaction, documentCatalogDiskPath, LockOperation.Write);

                    var physicalDocument = documentCatalog.GetById(newId);
                    if (physicalDocument != null)
                    {
                        string documentDiskPath = Path.Combine(physicalSchema.DiskPath, Helpers.GetDocumentModFilePath(physicalDocument.Id));

                        core.IO.DeleteFile(txRef.Transaction, documentDiskPath);

                        documentCatalog.Remove(physicalDocument);

                        core.Indexes.DeleteDocumentFromIndexes(txRef.Transaction, physicalSchema, physicalDocument.Id);

                        core.IO.PutJson(txRef.Transaction, documentCatalogDiskPath, documentCatalog);
                    }

                    txRef.Commit();

                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteDelete for process {processId}.", ex);
                throw;
            }
            */
        }

        #endregion

        #region API Handlers.

        public KbQueryResult ExecuteSample(ulong processId, string schemaName, int rowLimit)
        {
            try
            {
                var result = new KbQueryResult();

                using (var txRef = core.Transactions.Begin(processId))
                {
                    result = StaticSearcherMethods.SampleSchemaDocuments(core, txRef.Transaction, schemaName, rowLimit);
                    txRef.Commit();

                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }

        public KbQueryResult ExecuteList(ulong processId, string schemaName, int rowLimit = -1)
        {
            try
            {
                var result = new KbQueryResult();

                using (var txRef = core.Transactions.Begin(processId))
                {
                    result = StaticSearcherMethods.ListSchemaDocuments(core, txRef.Transaction, schemaName, rowLimit);
                    txRef.Commit();
                    result.Metrics = txRef.Transaction.PT?.ToCollection();
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
        public KbActionResponse Store(ulong processId, string schema, KbDocument document)
        {
            try
            {
                var result = new KbActionResponse();

                var physicalDocument = PhysicalDocument.FromPayload(document);
                physicalDocument.Created = DateTime.UtcNow;
                physicalDocument.Modfied = DateTime.UtcNow;

                using (var txRef = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schema, LockOperation.Write);

                    PutDocument(txRef.Transaction, physicalSchema, physicalDocument);

                    txRef.Commit();

                    result.Metrics = txRef.Transaction.PT?.ToCollection();
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
        /// Deletes a document by its ID.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <param name="newId"></param>
        /// <exception cref="KbObjectNotFoundException"></exception>
        public KbActionResponse DeleteById(ulong processId, string schema, Guid newId)
        {
            throw new KbNotImplementedException();

            /*
            try
            {
                var result = new KbActionResponse();
                using (var txRef = core.Transactions.Begin(processId))
                {

                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schema, LockOperation.Write);

                    string documentCatalogDiskPath = Path.Combine(physicalSchema.DiskPath, DocumentCatalogFile);

                    var documentCatalog = core.IO.GetJson<PhysicalDocumentCatalog>(txRef.Transaction, documentCatalogDiskPath, LockOperation.Write);


                    var physicalDocument = documentCatalog.GetById(newId);
                    if (physicalDocument != null)
                    {
                        string documentDiskPath = Path.Combine(physicalSchema.DiskPath, Helpers.GetDocumentModFilePath(physicalDocument.Id));

                        core.IO.DeleteFile(txRef.Transaction, documentDiskPath);

                        documentCatalog.Remove(physicalDocument);

                        core.Indexes.DeleteDocumentFromIndexes(txRef.Transaction, physicalSchema, physicalDocument.Id);

                        core.IO.PutJson(txRef.Transaction, documentCatalogDiskPath, documentCatalog);
                    }

                    txRef.Commit();

                    result.Metrics = txRef.Transaction.PT?.ToCollection();
                }
                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete document by ID for process {processId}.", ex);
                throw;
            }
            */
        }

        /// <summary>
        /// Returns a list of all documents in a schema.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        /// <exception cref="KbObjectNotFoundException"></exception>
        public KbDocumentCatalogCollection EnumerateCatalog(ulong processId, string schema)
        {
            throw new KbNotImplementedException();

            /*
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schema, LockOperation.Read);

                    var filePath = Path.Combine(physicalSchema.DiskPath, DocumentCatalogFile);
                    var documentCatalog = core.IO.GetJson<PhysicalDocumentCatalog>(txRef.Transaction, filePath, LockOperation.Read);

                    var result = new KbDocumentCatalogCollection();
                    foreach (var item in documentCatalog.Collection)
                    {
                        result.Add(item.ToPayload());
                    }
                    txRef.Commit();

                    result.Metrics = txRef.Transaction.PT?.ToCollection();

                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to get catalog for process {processId}.", ex);
                throw;
            }
            */
        }

        #endregion

        #region Core put/get methods.

        /// <summary>
        /// When we want to read a document we do it here - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documentId"></param>
        internal PhysicalDocument GetDocument(Transaction transaction, PhysicalSchema physicalSchema, uint documentId, LockOperation lockIntention)
        {
            //Open the document page catalog:
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
        internal PhysicalDocument GetDocument(Transaction transaction, PhysicalSchema physicalSchema, PageDocument pageDocument, LockOperation lockIntention)
        {
            var physicalDocumentPage = core.IO.GetJson<PhysicalDocumentPage>(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(pageDocument), lockIntention);
            return physicalDocumentPage.Documents.First(o => o.Key == pageDocument.DocumentId).Value;
        }

        internal IEnumerable<PageDocument> GetPageDocuments(Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockIntention)
        {
            //Open the document page catalog:
            var documentPageCatalog = core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogDiskPath(), lockIntention);
            return documentPageCatalog.ConsolidatedPageDocuments();
        }

        internal PhysicalDocumentPageCatalog GetDocumentPageCatalog(Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockIntention)
        {
            //Open the document page catalog:
            var documentPageCatalog = core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogDiskPath(), lockIntention);
            return documentPageCatalog;
        }

        /// <summary>
        /// When we want to write a document to the disk, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void PutDocument(Transaction transaction, PhysicalSchema physicalSchema, PhysicalDocument physicalDocument)
        {
            PhysicalDocumentPage documentPage;

            //Open the document page catalog:
            var documentPageCatalog = core.IO.GetJson<PhysicalDocumentPageCatalog>(transaction, physicalSchema.DocumentPageCatalogDiskPath(), LockOperation.Write);
            Utility.EnsureNotNull(documentPageCatalog);

            physicalDocument.Id = documentPageCatalog.ConsumeNextDocumentId();

            //Get the page that the document current exists in, if any.
            var physicalPageMap = documentPageCatalog.GetDocumentPageMap(physicalDocument.Id);
            if (physicalPageMap == null)
            {
                //The document doesnt exist in any pages, see if we can find one with some room:
                physicalPageMap = documentPageCatalog.GetPageWithRoomForNewDocument(core.Settings.DocumentPageSize);

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
            }
            else
            {
                //We found a page that contains the document, we need to open the page and modify the document with the given document id.
                documentPage = core.IO.GetJson<PhysicalDocumentPage>(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(physicalPageMap), LockOperation.Write);
                documentPage.Documents[physicalDocument.Id] = physicalDocument;
            }

            //Save the document page:
            core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogItemDiskPath(physicalPageMap), documentPage);

            //Save the docuemnt page catalog:
            core.IO.PutJson(transaction, physicalSchema.DocumentPageCatalogDiskPath(), documentPageCatalog);

            //Update all of the indexes that referecne the document.
            core.Indexes.InsertDocumentIntoIndexes(transaction, physicalSchema, physicalDocument, new PageDocument(documentPage.PageNumber, physicalDocument.Id));
        }

        #endregion
    }
}
