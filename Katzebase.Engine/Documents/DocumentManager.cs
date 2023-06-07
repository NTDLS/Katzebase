using Katzebase.Engine.Documents.Threading.MultiSchemaQuery.SchemaMapping;
using Katzebase.Engine.Documents.Threading.SingleSchemaQuery;
using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
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

        public KbQueryResult ExecuteExplain(ulong processId, PreparedQuery preparedQuery)
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

                    var lookupOptimization = core.Indexes.SelectIndexesForConditionLookupOptimization(txRef.Transaction, schemaMeta, preparedQuery.Conditions);
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

        public KbQueryResult ExecuteSelect(ulong processId, PreparedQuery preparedQuery)
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
                    result = FindDocumentsByPreparedQuery(pt, txRef.Transaction, preparedQuery);
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

        public KbActionResponse ExecuteDelete(ulong processId, PreparedQuery preparedQuery)
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

                    result = FindDocumentsByPreparedQuery(pt, txRef.Transaction, preparedQuery);

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
        /// Finds all document using a prepared query.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="KbInvalidSchemaException"></exception>
        private KbQueryResult FindDocumentsByPreparedQuery(PerformanceTrace? pt, Transaction transaction, PreparedQuery query)
        {
            var result = new KbQueryResult();

            if (query.SelectFields.Count == 1 && query.SelectFields[0].Key == "*")
            {
                query.SelectFields.Clear();
                throw new KbNotImplementedException("Select * is not implemented. This will require schema scampling.");
            }
            else if (query.SelectFields.Count == 0)
            {
                query.SelectFields.Clear();
                throw new KbNotImplementedException("No fields were selected.");
            }

            if (query.Schemas.Count > 1)
            {
                var schemaMap = new QuerySchemaMap();

                foreach (var querySchema in query.Schemas)
                {
                    //Lock the schema:
                    var ptLockSchema = pt?.BeginTrace<PersistSchema>(PerformanceTraceType.Lock);
                    var schemaMeta = core.Schemas.VirtualPathToMeta(transaction, querySchema.Name, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(querySchema.Name);
                    }
                    ptLockSchema?.EndTrace();
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    //Lock the document catalog:
                    var documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, DocumentCatalogFile);
                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(pt, transaction, documentCatalogDiskPath, LockOperation.Read);
                    Utility.EnsureNotNull(documentCatalog);

                    schemaMap.Add(querySchema.Alias, schemaMeta, documentCatalog, querySchema.Conditions);
                }

                //Figure out which indexes could assist us in retrieving the desired documents (if any).
                var ptOptimization = pt?.BeginTrace(PerformanceTraceType.Optimization);
                var lookupOptimization = core.Indexes.SelectIndexesForConditionLookupOptimization(transaction, schemaMap.First().Value.SchemaMeta, query.Conditions);
                ptOptimization?.EndTrace();

                /*
                 *  We need to build a generic key/value dataset which is the combined fieldset from each inner joined document.
                 *  Then we use the conditions that were supplied to eliminate results from that dataset.
                */

                var schemaMapResults = StaticSchemaMapper.Join(core, pt, transaction, schemaMap, query, lookupOptimization);
            }
            else
            {
                var singleSchema = query.Schemas.First();

                var subsetResults = StaticSingleSchemaDocumentSearcher.GetSingleSchemaDocumentsByConditions(core, pt, transaction, singleSchema.Name, query);

                foreach (var field in query.SelectFields)
                {
                    result.Fields.Add(new KbQueryField(field.Alias));
                }

                foreach (var subsetResult in subsetResults.Collection)
                {
                    result.Rows.Add(new KbQueryRow(subsetResult.Values));
                }
            }

            return result;
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
