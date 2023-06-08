using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Searchers;
using Katzebase.Engine.Query.Searchers.SingleSchema;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Documents
{
    public class DocumentManager
    {
        private Core core;

        public DocumentManager(Core core)
        {
            this.core = core;
        }

        internal KbQueryResult ExecuteExplain(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbQueryResult();
                PerformanceTrace? pt = null;

                var session = core.Sessions.ByProcessId(processId);
                if (session.TraceWaitTimesEnabled)
                {
                    pt = new PerformanceTrace();
                }

                var ptAcquireTransaction = pt?.BeginTrace(PerformanceTraceType.AcquireTransaction);
                using (var txRef = core.Transactions.Begin(processId))
                {
                    ptAcquireTransaction?.EndTrace();

                    var ptLockSchema = pt?.BeginTrace<PersistSchema>(PerformanceTraceType.Lock);
                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, preparedQuery.Schemas[0].Alias, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(preparedQuery.Schemas[0].Alias);
                    }
                    ptLockSchema?.EndTrace();
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    var lookupOptimization = SSQStaticOptimization.SelectIndexesForConditionLookupOptimization(core, txRef.Transaction, schemaMeta, preparedQuery.Conditions);
                    result.Explanation = lookupOptimization.BuildFullVirtualExpression();

                    txRef.Commit(); //Not that we did any work.
                }

                if (session.TraceWaitTimesEnabled && pt != null)
                {
                    foreach (var wt in pt.Aggregations)
                    {
                        result.WaitTimes.Add(new KbNameValue<double>(wt.Key, wt.Value));
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteSelect(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbQueryResult();
                PerformanceTrace? pt = null;

                var session = core.Sessions.ByProcessId(processId);
                if (session.TraceWaitTimesEnabled)
                {
                    pt = new PerformanceTrace();
                }

                var ptAcquireTransaction = pt?.BeginTrace(PerformanceTraceType.AcquireTransaction);
                using (var txRef = core.Transactions.Begin(processId))
                {
                    ptAcquireTransaction?.EndTrace();
                    result = StaticSearcherMethods.FindDocumentsByPreparedQuery(core, pt, txRef.Transaction, preparedQuery);
                    txRef.Commit();
                }

                if (session.TraceWaitTimesEnabled && pt != null)
                {
                    foreach (var wt in pt.Aggregations)
                    {
                        result.WaitTimes.Add(new KbNameValue<double>(wt.Key, wt.Value));
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteDelete(ulong processId, PreparedQuery preparedQuery)
        {
            //TODO: This is a stub, this does NOT work.
            try
            {
                var result = new KbActionResponse();
                var pt = new PerformanceTrace();

                var ptAcquireTransaction = pt?.BeginTrace(PerformanceTraceType.AcquireTransaction);
                using (var txRef = core.Transactions.Begin(processId))
                {
                    ptAcquireTransaction?.EndTrace();

                    result = StaticSearcherMethods.FindDocumentsByPreparedQuery(core, pt, txRef.Transaction, preparedQuery);

                    //TODO: Delete the documents.

                    /*
                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(txRef.Transaction, documentCatalogDiskPath, LockOperation.Write);

                    var persistDocument = documentCatalog.GetById(newId);
                    if (persistDocument != null)
                    {
                        string documentDiskPath = Path.Combine(schemaMeta.DiskPath, Helpers.GetDocumentModFilePath(persistDocument.Id));

                        core.IO.DeleteFile(txRef.Transaction, documentDiskPath);

                        documentCatalog.Remove(persistDocument);

                        core.Indexes.DeleteDocumentFromIndexes(txRef.Transaction, schemaMeta, persistDocument.Id);

                        core.IO.PutJson(txRef.Transaction, documentCatalogDiskPath, documentCatalog);
                    }
                    */

                    txRef.Commit();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteDelete for process {processId}.", ex);
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
        /// <exception cref="KbInvalidSchemaException"></exception>
        public void Store(ulong processId, string schema, KbDocument document, out Guid? newId)
        {
            try
            {
                var persistDocument = PersistDocument.FromPayload(document);

                if (persistDocument.Id == null || persistDocument.Id == Guid.Empty)
                {
                    persistDocument.Id = Guid.NewGuid();
                }
                if (persistDocument.Created == null || persistDocument.Created == DateTime.MinValue)
                {
                    persistDocument.Created = DateTime.UtcNow;
                }
                if (persistDocument.Modfied == null || persistDocument.Modfied == DateTime.MinValue)
                {
                    persistDocument.Modfied = DateTime.UtcNow;
                }

                using (var txRef = core.Transactions.Begin(processId))
                {
                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Write);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(schema);
                    }
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    string documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, DocumentCatalogFile);

                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(txRef.Transaction, documentCatalogDiskPath, LockOperation.Write);
                    Utility.EnsureNotNull(documentCatalog);

                    documentCatalog.Add(persistDocument);
                    core.IO.PutJson(txRef.Transaction, documentCatalogDiskPath, documentCatalog);

                    string documentDiskPath = Path.Combine(schemaMeta.DiskPath, Helpers.GetDocumentModFilePath((Guid)persistDocument.Id));
                    core.IO.CreateDirectory(txRef.Transaction, Path.GetDirectoryName(documentDiskPath));
                    core.IO.PutJson(txRef.Transaction, documentDiskPath, persistDocument);

                    core.Indexes.InsertDocumentIntoIndexes(txRef.Transaction, schemaMeta, persistDocument);

                    newId = persistDocument.Id;

                    txRef.Commit();
                }
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
        /// <exception cref="KbInvalidSchemaException"></exception>
        public void DeleteById(ulong processId, string schema, Guid newId)
        {
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Write);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(schema);
                    }

                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    string documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, DocumentCatalogFile);

                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(txRef.Transaction, documentCatalogDiskPath, LockOperation.Write);

                    Utility.EnsureNotNull(documentCatalog);

                    var persistDocument = documentCatalog.GetById(newId);
                    if (persistDocument != null)
                    {
                        string documentDiskPath = Path.Combine(schemaMeta.DiskPath, Helpers.GetDocumentModFilePath(persistDocument.Id));

                        core.IO.DeleteFile(txRef.Transaction, documentDiskPath);

                        documentCatalog.Remove(persistDocument);

                        core.Indexes.DeleteDocumentFromIndexes(txRef.Transaction, schemaMeta, persistDocument.Id);

                        core.IO.PutJson(txRef.Transaction, documentCatalogDiskPath, documentCatalog);
                    }

                    txRef.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete document by ID for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns a list of all documents in a schema.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        /// <exception cref="KbInvalidSchemaException"></exception>
        public List<PersistDocumentCatalogItem> EnumerateCatalog(ulong processId, string schema)
        {
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    PersistSchema schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(schema);
                    }
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    var filePath = Path.Combine(schemaMeta.DiskPath, DocumentCatalogFile);
                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(txRef.Transaction, filePath, LockOperation.Read);
                    Utility.EnsureNotNull(documentCatalog);

                    var list = new List<PersistDocumentCatalogItem>();
                    foreach (var item in documentCatalog.Collection)
                    {
                        list.Add(item);
                    }
                    txRef.Commit();

                    return list;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to get catalog for process {processId}.", ex);
                throw;
            }
        }
    }
}
