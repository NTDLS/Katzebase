using Katzebase.Engine.Documents.Threading;
using Katzebase.Engine.Indexes;
using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Transactions;
using Katzebase.Library;
using Katzebase.Library.Exceptions;
using Katzebase.Library.Payloads;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using static Katzebase.Engine.Constants;
using static Katzebase.Engine.Documents.Threading.DocumentThreadingConstants;

namespace Katzebase.Engine.Documents
{
    public class DocumentManager
    {
        private Core core;

        public DocumentManager(Core core)
        {
            this.core = core;
        }

        public KbQueryResult ExecuteSelect(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbQueryResult(); ;

                using (var transaction = core.Transactions.Begin(processId))
                {
                    result = FindDocumentsByPreparedQuery(transaction.Transaction, preparedQuery);
                    transaction.Commit();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete document by ID for process {processId}.", ex);
                throw;
            }
        }

        public KbActionResponse ExecuteDelete(ulong processId, PreparedQuery preparedQuery)
        {
            //TODO: This is a stub, this does NOT work.
            try
            {
                var result = new KbActionResponse();

                using (var transaction = core.Transactions.Begin(processId))
                {
                    result = FindDocumentsByPreparedQuery(transaction.Transaction, preparedQuery);

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

                    transaction.Commit();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete document by ID for process {processId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets all documents by a subset of conditions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="documentCatalogItems"></param>
        /// <param name="schemaMeta"></param>
        /// <param name="query"></param>
        /// <param name="lookupOptimization"></param>
        /// <returns></returns>
        private DocumentLookupResults GetAllDocumentsByConditions(Transaction transaction,
            List<PersistDocumentCatalogItem> documentCatalogItems, PersistSchema schemaMeta, PreparedQuery query,
            ConditionLookupOptimization lookupOptimization)
        {
            /*
            //indexing limits the documents we need to scan.
            if (rootCondition.IndexSelection != null)
            {
                Utility.EnsureNotNull(rootCondition.IndexSelection.Index.DiskPath);
                var indexPageCatalog = core.IO.GetPBuf<PersistIndexPageCatalog>(transaction, rootCondition.IndexSelection.Index.DiskPath, LockOperation.Read);
                Utility.EnsureNotNull(indexPageCatalog);
                limitingDocumentIds = core.Indexes.MatchDocuments(indexPageCatalog, rootCondition.IndexSelection, rootCondition);
                if (limitingDocumentIds?.Count == 0)
                {
                    limitingDocumentIds = null;
                }
            }
            */

            var virtualExpression = lookupOptimization.BuildFullVirtualExpression();

            Console.WriteLine(virtualExpression);

            if (lookupOptimization.CanApplyIndexing())
            {
                //All condition subsets have a selected index. Start building a list of possible document IDs.
                foreach (var subset in lookupOptimization.Conditions.NonRootSubsets)
                {

                }
            }
            else
            {
                /* One or more of the conditon subsets lacks an index.
                 *
                 *   Since indexing requires that we can ensure document elimination, we will have
                 *      to ensure that we have a covering index on EACH-and-EVERY conditon group.
                 *
                 *   Then we can search the indexes for each condition group to obtain a list of all possible document IDs,
	             *       then use those document IDs to early eliminate documents from the main lookup loop.
                 *
                 *   If any one conditon group does not have an index, then no indexing will be used at all since all documents
	             *       will need to be scaned anyway. To prevent unindexed scans, reduce the number of condition groups (nested in parentheses).
	             *
                 * ConditionLookupOptimization:BuildFullVirtualExpression() Will tell you why we cant use an index.
                 * var explanationOfIndexability = lookupOptimization.BuildFullVirtualExpression();
	             *
                */
            }

            //return new DocumentLookupResults();

            var threads = new DocumentLookupThreads(transaction, schemaMeta, query, lookupOptimization, DocumentLookupThreadProc);

            int maxThreads = 1;
            if (documentCatalogItems.Count > 100)
            {
                maxThreads = Environment.ProcessorCount * 2;
            }

            threads.InitializePool(maxThreads);

            //Loop through each document in the catalog:
            foreach (var documentCatalogItem in documentCatalogItems)
            {
                threads.Enqueue(documentCatalogItem);
            }

            threads.WaitOnThreadCompletion();

            threads.Stop();

            return threads.Results;
        }


        /// <summary>
        /// Thread proc for looking up documents. These are started in a batch per-query and listen for lookup requests.
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="KbParserException"></exception>
        private void DocumentLookupThreadProc(object? obj)
        {
            Utility.EnsureNotNull(obj);

            var param = (DocumentLookupThreadParam)obj;
            var expression = new NCalc.Expression(param.LookupOptimization.Conditions.Root.Expression);

            Utility.EnsureNotNull(param.SchemaMeta.DiskPath);

            var slot = param.ThreadSlots[param.ThreadSlotNumber];

            while (true)
            {
                slot.State = DocumentLookupThreadState.Ready;
                slot.Event.WaitOne();

                if (slot.State == DocumentLookupThreadState.Shutdown)
                {
                    return;
                }

                slot.State = DocumentLookupThreadState.Executing;

                var persistDocumentDiskPath = Path.Combine(param.SchemaMeta.DiskPath, slot.DocumentCatalogItem.FileName);

                var persistDocument = core.IO.GetJson<PersistDocument>(param.Transaction, persistDocumentDiskPath, LockOperation.Read);
                Utility.EnsureNotNull(persistDocument);
                Utility.EnsureNotNull(persistDocument.Content);

                var jContent = JObject.Parse(persistDocument.Content);

                //If we have subsets, then we need to satisify those in order to complete the equation.
                foreach (var subsetKey in param.LookupOptimization.Conditions.Root.SubsetKeys)
                {
                    var subExpression = param.LookupOptimization.Conditions.SubsetByKey(subsetKey);
                    bool subExpressionResult = SatisifySubExpression(param.LookupOptimization, jContent, subExpression);
                    expression.Parameters[subsetKey] = subExpressionResult;
                }

                if ((bool)expression.Evaluate())
                {
                    Utility.EnsureNotNull(persistDocument.Id);
                    var result = new DocumentLookupResult((Guid)persistDocument.Id);

                    foreach (string field in param.Query.SelectFields)
                    {
                        if (jContent.TryGetValue(field, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                        {
                            result.Values.Add(jToken.ToString());
                        }
                        else
                        {
                            result.Values.Add(string.Empty);
                        }
                    }

                    param.Results.Add(result);
                }
            }
        }

        /// <summary>
        /// Mathematically collapses all subexpressions to return a boolean match.
        /// </summary>
        /// <param name="lookupOptimization"></param>
        /// <param name="jContent"></param>
        /// <param name="conditionSubset"></param>
        /// <returns></returns>
        /// <exception cref="KbParserException"></exception>
        private bool SatisifySubExpression(ConditionLookupOptimization lookupOptimization, JObject jContent, ConditionSubset conditionSubset)
        {
            var expression = new NCalc.Expression(conditionSubset.Expression);

            //If we have subsets, then we need to satisify those in order to complete the equation.
            foreach (var subsetKey in conditionSubset.SubsetKeys)
            {
                var subExpression = lookupOptimization.Conditions.SubsetByKey(subsetKey);
                bool subExpressionResult = SatisifySubExpression(lookupOptimization, jContent, subExpression);
                expression.Parameters[subsetKey] = subExpressionResult;
            }

            foreach (var condition in conditionSubset.Conditions)
            {
                Utility.EnsureNotNull(condition.Left.Value);

                //Get the value of the condition:
                if (jContent.TryGetValue(condition.Left.Value, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                {
                    expression.Parameters[condition.ConditionKey] = condition.IsMatch(jToken.ToString().ToLower());
                }
                else
                {
                    throw new KbParserException($"Field not found in document [{condition.Left.Value}].");
                }
            }

            return (bool)expression.Evaluate();
        }

        /// <summary>
        /// Finds all document using a prepared query.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="KbSchemaDoesNotExistException"></exception>
        private KbQueryResult FindDocumentsByPreparedQuery(Transaction transaction, PreparedQuery query)
        {
            var result = new KbQueryResult();

            //Lock the schema:
            var schemaMeta = core.Schemas.VirtualPathToMeta(transaction, query.Schema, LockOperation.Read);
            if (schemaMeta == null || schemaMeta.Exists == false)
            {
                throw new KbSchemaDoesNotExistException(query.Schema);
            }
            Utility.EnsureNotNull(schemaMeta.DiskPath);

            //Lock the document catalog:
            var documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, Constants.DocumentCatalogFile);
            var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(transaction, documentCatalogDiskPath, LockOperation.Read);
            Utility.EnsureNotNull(documentCatalog);

            foreach (var field in query.SelectFields)
            {
                result.Fields.Add(new KbQueryField(field));
            }

            //Figure out which indexes could assist us in retrieving the desired documents (if any).
            var lookupOptimization = core.Indexes.SelectIndexesForConditionLookupOptimization(transaction, schemaMeta, query.Conditions);

            var subsetResults = GetAllDocumentsByConditions(transaction, documentCatalog.Collection, schemaMeta, query, lookupOptimization);

            foreach (var subsetResult in subsetResults.Collection)
            {
                result.Rows.Add(new KbQueryRow(subsetResult.Values));
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
        /// <exception cref="KbSchemaDoesNotExistException"></exception>
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
                        throw new KbSchemaDoesNotExistException(schema);
                    }
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    string documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, Constants.DocumentCatalogFile);

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
        /// <exception cref="KbSchemaDoesNotExistException"></exception>
        public void DeleteById(ulong processId, string schema, Guid newId)
        {
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Write);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbSchemaDoesNotExistException(schema);
                    }

                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    string documentCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, Constants.DocumentCatalogFile);

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
        /// <exception cref="KbSchemaDoesNotExistException"></exception>
        public List<PersistDocumentCatalogItem> EnumerateCatalog(ulong processId, string schema)
        {
            try
            {
                using (var transaction = core.Transactions.Begin(processId))
                {
                    PersistSchema schemaMeta = core.Schemas.VirtualPathToMeta(transaction.Transaction, schema, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbSchemaDoesNotExistException(schema);
                    }
                    Utility.EnsureNotNull(schemaMeta.DiskPath);

                    var filePath = Path.Combine(schemaMeta.DiskPath, Constants.DocumentCatalogFile);
                    var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(transaction.Transaction, filePath, LockOperation.Read);
                    Utility.EnsureNotNull(documentCatalog);

                    var list = new List<PersistDocumentCatalogItem>();
                    foreach (var item in documentCatalog.Collection)
                    {
                        list.Add(item);
                    }
                    transaction.Commit();

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
