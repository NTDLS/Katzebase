using Katzebase.Engine.Documents;
using Katzebase.Engine.Indexes.Matching;
using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Threading;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json.Linq;
using static Katzebase.Engine.Indexes.Matching.IndexConstants;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Indexes.Management
{
    /// <summary>
    /// Provides core index management functionality. Reading, writing, locking, listing, etc.
    /// </summary>
    public class IndexManager
    {
        private Core core;
        internal IndexQueryHandlers QueryHandlers { get; set; }
        public IndexAPIHandlers APIHandlers { get; set; }

        public IndexManager(Core core)
        {
            this.core = core;
            QueryHandlers = new IndexQueryHandlers(core);
            APIHandlers = new IndexAPIHandlers(core);
        }

        internal void CreateIndex(Transaction transaction, string schemaName, KbIndex index, out Guid newId)
        {
            var physicalIndex = PhysicalIndex.FromClientPayload(index);

            physicalIndex.Id = Guid.NewGuid();
            physicalIndex.Created = DateTime.UtcNow;
            physicalIndex.Modfied = DateTime.UtcNow;

            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Write);

            if (indexCatalog.GetByName(index.Name) != null)
            {
                throw new KbObjectAlreadysExistsException(index.Name);
            }

            indexCatalog.Add(physicalIndex);

            if (indexCatalog.DiskPath == null || physicalSchema.DiskPath == null)
            {
                throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
            }

            core.IO.PutJson(transaction, indexCatalog.DiskPath, indexCatalog);
            physicalIndex.DiskPath = Path.Combine(physicalSchema.DiskPath, MakeIndexFileName(index.Name));
            core.IO.PutPBuf(transaction, physicalIndex.DiskPath, new PhysicalIndexPages());

            RebuildIndex(transaction, physicalSchema, physicalIndex);

            newId = physicalIndex.Id;
        }

        internal void RebuildIndex(Transaction transaction, string schemaName, string indexName)
        {
            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Write);
            if (indexCatalog.DiskPath == null || physicalSchema.DiskPath == null)
            {
                throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
            }

            var physicalIindex = indexCatalog.GetByName(indexName) ?? throw new KbObjectNotFoundException(indexName);
            physicalIindex.DiskPath = Path.Combine(physicalSchema.DiskPath, MakeIndexFileName(physicalIindex.Name));

            RebuildIndex(transaction, physicalSchema, physicalIindex);
        }

        internal void DropIndex(Transaction transaction, string schemaName, string indexName)
        {
            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Write);
            if (indexCatalog.DiskPath == null || physicalSchema.DiskPath == null)
            {
                throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
            }

            var physicalIindex = indexCatalog.GetByName(indexName) ?? throw new KbObjectNotFoundException(indexName);
            indexCatalog.Remove(physicalIindex);

            physicalIindex.DiskPath = Path.Combine(physicalSchema.DiskPath, MakeIndexFileName(physicalIindex.Name));

            core.IO.DeleteFile(transaction, physicalIindex.DiskPath);
            core.IO.PutJson(transaction, indexCatalog.DiskPath, indexCatalog);
        }

        internal Dictionary<uint, DocumentPointer> MatchDocuments(Transaction transaction, PhysicalIndexPages physicalIndexPages,
            IndexSelection indexSelection, ConditionSubset conditionSubset, Dictionary<string, string> conditionValues)
        {
            var workingPhysicalIndexLeaf = physicalIndexPages.Root;
            var lastFoundPhysicalIndexLeaf = workingPhysicalIndexLeaf;

            bool foundAnything = false;

            foreach (var attribute in indexSelection.Index.Attributes)
            {
                Utility.EnsureNotNull(attribute.Field);
                var conditionField = conditionSubset.Conditions.Where(o => o.Left.Value == attribute.Field.ToLowerInvariant()).FirstOrDefault();
                if (conditionField == null)
                {
                    //This happends when there is no condition on this index, this will be a partial match.
                    break;
                }

                if (conditionField.LogicalConnector == LogicalConnector.Or)
                {
                    //TODO: Indexing only supports AND connectors, thats a performance problem.
                    break;
                }

                Utility.EnsureNotNull(workingPhysicalIndexLeaf);

                var ptIndexSeek = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.IndexSearch);

                var conditionValue = conditionValues[attribute.Field.ToLower()];
                if (conditionField.LogicalQualifier == LogicalQualifier.Equals)
                    lastFoundPhysicalIndexLeaf = workingPhysicalIndexLeaf.Children.FirstOrDefault(o => o.Key == conditionValue).Value;
                else if (conditionField.LogicalQualifier == LogicalQualifier.NotEquals)
                    lastFoundPhysicalIndexLeaf = workingPhysicalIndexLeaf.Children.FirstOrDefault(o => o.Key != conditionValue).Value;
                else throw new KbNotImplementedException($"Condition qualifier {conditionField.LogicalQualifier} has not been implemented.");

                ptIndexSeek?.StopAndAccumulate();

                if (lastFoundPhysicalIndexLeaf == null)
                {
                    break;
                }

                foundAnything = true;

                if (lastFoundPhysicalIndexLeaf?.Documents?.Any() == true) //If we are at the base of the tree then there is no need to go further down.
                {
                    return lastFoundPhysicalIndexLeaf.Documents.ToDictionary(o => o.DocumentId, o => new DocumentPointer(o.PageNumber, o.DocumentId));
                }
                else
                {
                    workingPhysicalIndexLeaf = lastFoundPhysicalIndexLeaf;
                }
            }

            if (foundAnything == false)
            {
                return new Dictionary<uint, DocumentPointer>();
            }

            Utility.EnsureNotNull(workingPhysicalIndexLeaf);

            var ptIndexDistillation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.IndexDistillation);
            //If we got here then we didnt get a full match and will need to add all of the child-leaf document IDs for later elimination.
            var resultintDocuments = DistillIndexLeaves(workingPhysicalIndexLeaf);
            ptIndexDistillation?.StopAndAccumulate();

            return resultintDocuments;
        }

        /// <summary>
        /// Finds document IDs given a set of conditions.
        /// </summary>
        internal Dictionary<uint, DocumentPointer> MatchDocuments(Transaction transaction,
            PhysicalIndexPages physicalIndexPages, IndexSelection indexSelection, ConditionSubset conditionSubset, string workingSchemaPrefix)
        {
            var workingPhysicalIndexLeaf = physicalIndexPages.Root;
            var lastFoundPhysicalIndexLeaf = workingPhysicalIndexLeaf;

            bool foundAnything = false;

            foreach (var attribute in indexSelection.Index.Attributes)
            {
                Utility.EnsureNotNull(attribute.Field);
                var conditionField = conditionSubset.Conditions
                    .Where(o => o.Left.Prefix == workingSchemaPrefix && o.Left.Value == attribute.Field.ToLowerInvariant()).FirstOrDefault();
                if (conditionField == null)
                {
                    //This happends when there is no condition on this index, this will be a partial match.
                    break;
                }

                if (conditionField.LogicalConnector == LogicalConnector.Or)
                {
                    //TODO: Indexing only supports AND connectors, thats a performance problem.
                    break;
                }

                Utility.EnsureNotNull(workingPhysicalIndexLeaf);

                var ptIndexSeek = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.IndexSearch);

                if (conditionField.LogicalQualifier == LogicalQualifier.Equals)
                    lastFoundPhysicalIndexLeaf = workingPhysicalIndexLeaf.Children.FirstOrDefault(o => o.Key == conditionField.Right.Value).Value;
                else if (conditionField.LogicalQualifier == LogicalQualifier.NotEquals)
                    lastFoundPhysicalIndexLeaf = workingPhysicalIndexLeaf.Children.FirstOrDefault(o => o.Key != conditionField.Right.Value).Value;
                else throw new KbNotImplementedException($"Condition qualifier {conditionField.LogicalQualifier} has not been implemented.");

                ptIndexSeek?.StopAndAccumulate();

                if (lastFoundPhysicalIndexLeaf == null)
                {
                    break;
                }

                foundAnything = true;

                if (lastFoundPhysicalIndexLeaf?.Documents?.Any() == true) //If we are at the base of the tree then there is no need to go further down.
                {
                    return lastFoundPhysicalIndexLeaf.Documents.ToDictionary(o => o.DocumentId, o => new DocumentPointer(o.PageNumber, o.DocumentId));
                }
                else
                {
                    workingPhysicalIndexLeaf = lastFoundPhysicalIndexLeaf;
                }
            }

            if (foundAnything == false)
            {
                return new Dictionary<uint, DocumentPointer>();
            }

            Utility.EnsureNotNull(workingPhysicalIndexLeaf);

            //This is an index scan.
            var ptIndexDistillation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.IndexDistillation);
            //If we got here then we didnt get a full match and will need to add all of the child-leaf document IDs for later elimination.
            var resultintDocuments = DistillIndexLeaves(workingPhysicalIndexLeaf);
            ptIndexDistillation?.StopAndAccumulate();

            return resultintDocuments;
        }

        /// <summary>
        /// Traverse to the bottom of the index tree (from whatever starting point is passed in) and return a list of all documentids.
        /// </summary>
        /// <param name="indexEntires"></param>
        /// <returns></returns>
        private Dictionary<uint, DocumentPointer> DistillIndexLeaves(PhysicalIndexLeaf physicalIndexLeaf)
        {
            var result = new List<DocumentPointer>();

            void DistillIndexLeavesRecursive(PhysicalIndexLeaf physicalIndexLeaf)
            {
                foreach (var child in physicalIndexLeaf.Children)
                {
                    DistillIndexLeavesRecursive(child.Value);
                }

                if (physicalIndexLeaf?.Documents?.Any() == true)
                {
                    result.AddRange(physicalIndexLeaf.Documents.Select(o => new DocumentPointer(o.PageNumber, o.DocumentId)));
                }
            }

            if (physicalIndexLeaf?.Documents?.Any() == true)
            {
                result.AddRange(physicalIndexLeaf.Documents.Select(o => new DocumentPointer(o.PageNumber, o.DocumentId)));
            }
            else if (physicalIndexLeaf?.Children != null)
            {
                foreach (var child in physicalIndexLeaf.Children)
                {
                    DistillIndexLeavesRecursive(child.Value);
                }
            }

            return result.ToDictionary(o => o.DocumentId, o => o);
        }

        internal PhysicalIndexCatalog AcquireIndexCatalog(Transaction transaction, string schemaName, LockOperation intendedOperation)
        {
            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, intendedOperation);
            return AcquireIndexCatalog(transaction, physicalSchema, intendedOperation);
        }

        public string MakeIndexFileName(string indexName)
        {
            return $"@Index_{0}_Pages_{Helpers.MakeSafeFileName(indexName)}.PBuf";
        }

        internal PhysicalIndexCatalog AcquireIndexCatalog(Transaction transaction, PhysicalSchema physicalSchema, LockOperation intendedOperation)
        {
            string indexCatalogDiskPath = Path.Combine(physicalSchema.DiskPath, IndexCatalogFile);

            var indexCatalog = core.IO.GetJson<PhysicalIndexCatalog>(transaction, indexCatalogDiskPath, intendedOperation);

            indexCatalog.DiskPath = indexCatalogDiskPath;

            foreach (var index in indexCatalog.Collection)
            {
                index.DiskPath = Path.Combine(physicalSchema.DiskPath, MakeIndexFileName(index.Name));
            }

            return indexCatalog;
        }

        private List<string> GetIndexSearchTokens(Transaction transaction, PhysicalIndex physicalIindex, PhysicalDocument document)
        {
            try
            {
                var result = new List<string>();

                foreach (var indexAttribute in physicalIindex.Attributes)
                {
                    Utility.EnsureNotNull(indexAttribute.Field);

                    var jsonContent = JObject.Parse(document.Content);
                    if (jsonContent.TryGetValue(indexAttribute.Field, StringComparison.CurrentCultureIgnoreCase, out JToken? jToken))
                    {
                        result.Add(jToken.ToString().ToLower());
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to build index search tokens for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Finds the appropriate index page for a set of key values in the given index page catalog.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalIindex"></param>
        /// <param name="searchTokens"></param>
        /// <param name="indexPageCatalog"></param>
        /// <returns>A reference to a node in the suppliedIndexPageCatalog</returns>
        private IndexScanResult LocateExtentInGivenIndexPageCatalog(Transaction transaction, List<string> searchTokens, PhysicalIndexPages rootPhysicalIndexPages)
        {
            try
            {
                var physicalIndexPages = rootPhysicalIndexPages;

                var result = new IndexScanResult()
                {
                    Leaf = physicalIndexPages.Root,
                    MatchType = IndexMatchType.None
                };

                if (physicalIndexPages.Root.Children.Count == 0)
                {
                    return result; //The index is empty.
                }

                foreach (var token in searchTokens)
                {
                    if (result.Leaf.Children == null)
                    {
                        break;
                    }

                    if (result.Leaf.Children.ContainsKey(token))
                    {
                        result.ExtentLevel++;
                        result.Leaf = result.Leaf.Children[token]; //Move one level lower in the extent tree.
                    }
                }

                if (result.ExtentLevel > 0)
                {
                    result.MatchType = result.ExtentLevel == searchTokens.Count ? IndexMatchType.Full : IndexMatchType.Partial;
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to locate key page for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Updates an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        private void UpdateDocumentIntoIndexes(Transaction transaction, PhysicalSchema physicalSchema, PhysicalDocument physicalDocument, DocumentPointer documentPointer)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                throw new KbNotImplementedException();

                //Loop though each index in the schema.
                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    //DeleteDocumentsFromIndex(transaction, physicalSchema, physicalIindex, documentPointer);
                    //InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, physicalDocument, documentPointer);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Multi-index insert failed for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void InsertDocumentIntoIndexes(Transaction transaction, PhysicalSchema physicalSchema, PhysicalDocument physicalDocument, DocumentPointer documentPointer)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, physicalDocument, documentPointer);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Multi-index insert failed for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts an index entry for a single document into a single index using the file name from the index object.
        /// </summary>
        private void InsertDocumentIntoIndex(Transaction transaction, PhysicalSchema physicalSchema,
            PhysicalIndex physicalIindex, PhysicalDocument document, DocumentPointer documentPointer)
        {
            var physicalIndexPages = core.IO.GetPBuf<PhysicalIndexPages>(transaction, physicalIindex.DiskPath, LockOperation.Write);
            InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, document, documentPointer, physicalIndexPages, true);
        }

        /// <summary>
        /// Inserts an index entry for a single document into a single index using a long lived index page catalog.
        /// </summary>
        private void InsertDocumentIntoIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIindex,
            PhysicalDocument document, DocumentPointer documentPointer, PhysicalIndexPages physicalIndexPages, bool flushPageCatalog)
        {
            try
            {
                var searchTokens = GetIndexSearchTokens(transaction, physicalIindex, document);
                var indexScanResult = LocateExtentInGivenIndexPageCatalog(transaction, searchTokens, physicalIndexPages);

                //If we found a full match for all supplied key values - add the document to the leaf collection.
                if (indexScanResult.MatchType == IndexMatchType.Full)
                {
                    Utility.EnsureNotNull(indexScanResult.Leaf);

                    indexScanResult.Leaf.Documents ??= new List<PhysicalIndexEntry>();

                    if (physicalIindex.IsUnique && indexScanResult.Leaf.Documents.Count > 1)
                    {
                        string exceptionText = $"Duplicate key violation occurred for index [{physicalSchema.VirtualPath}]/[{physicalIindex.Name}]. Values: {{{string.Join(",", searchTokens)}}}";
                        throw new KbDuplicateKeyViolationException(exceptionText);
                    }
                }
                else
                {
                    //If we didn't find a full match for all supplied key values, then create the tree and add the document to the
                    //  lowest leaf. Note that we are going to start creating the leaf level at the findResult.ExtentLevel. This is
                    //  because we may have a partial match and don't need to create the full tree.

                    for (int i = indexScanResult.ExtentLevel; i < searchTokens.Count; i++)
                    {
                        Utility.EnsureNotNull(indexScanResult?.Leaf);
                        indexScanResult.Leaf = indexScanResult.Leaf.AddNewLeaf(searchTokens[i]);
                    }

                    Utility.EnsureNotNull(indexScanResult?.Leaf);

                    indexScanResult.Leaf.Documents ??= new List<PhysicalIndexEntry>();
                }

                //Add the document to the lowest index extent.
                indexScanResult.Leaf.Documents.Add(new PhysicalIndexEntry(documentPointer.DocumentId, documentPointer.PageNumber));

                if (flushPageCatalog)
                {
                    core.IO.PutPBuf(transaction, physicalIindex.DiskPath, physicalIndexPages);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Index document insert failed for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #region Threading.

        private class RebuildIndexThreadParam
        {
            public Transaction Transaction { get; set; }
            public PhysicalSchema PhysicalSchema { get; set; }
            public PhysicalIndex PhysicalIindex { get; set; }
            public PhysicalIndexPages PhysicalIndexPages { get; set; }
            public object SyncObject { get; private set; } = new object();

            public RebuildIndexThreadParam(Transaction transaction, PhysicalSchema physicalSchema,
                PhysicalIndexPages physicalIndexPages, PhysicalIndex physicalIindex)
            {
                Transaction = transaction;
                PhysicalSchema = physicalSchema;
                PhysicalIindex = physicalIindex;
                PhysicalIndexPages = physicalIndexPages;
            }
        }

        #endregion

        private void RebuildIndexThreadProc(ThreadPoolQueue<DocumentPointer, RebuildIndexThreadParam> pool, RebuildIndexThreadParam? param)
        {
            Utility.EnsureNotNull(param);

            while (pool.ContinueToProcessQueue)
            {
                var documentPointer = pool.DequeueWorkItem();
                if (documentPointer == null)
                {
                    continue;
                }

                if (param.PhysicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null {nameof(param.PhysicalSchema.DiskPath)}.");
                }

                var PhysicalDocument = core.Documents.AcquireDocument(param.Transaction, param.PhysicalSchema, documentPointer.DocumentId, LockOperation.Read);

                lock (param.SyncObject)
                {
                    InsertDocumentIntoIndex(param.Transaction, param.PhysicalSchema, param.PhysicalIindex, PhysicalDocument, documentPointer, param.PhysicalIndexPages, false);
                }
            }
        }

        /// <summary>
        /// Inserts all documents in a schema into a single index in the schema. Locks the index page catalog for write.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="physicalIindex"></param>
        private void RebuildIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIindex)
        {
            try
            {
                var documentPointers = core.Documents.AcquireDocumentPointers(transaction, physicalSchema, LockOperation.Read).ToList();

                //Clear out the existing index pages.
                core.IO.PutPBuf(transaction, physicalIindex.DiskPath, new PhysicalIndexPages());

                var physicalIndexPages = core.IO.GetPBuf<PhysicalIndexPages>(transaction, physicalIindex.DiskPath, LockOperation.Write);

                var ptThreadCreation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCreation);
                var threadParam = new RebuildIndexThreadParam(transaction, physicalSchema, physicalIndexPages, physicalIindex);
                int threadCount = ThreadPoolHelper.CalculateThreadCount(core.Sessions.ByProcessId(transaction.ProcessId), documentPointers.Count);
                transaction.PT?.AddDescreteMetric(PerformanceTraceDescreteMetricType.ThreadCount, threadCount);
                var threadPool = ThreadPoolQueue<DocumentPointer, RebuildIndexThreadParam>
                    .CreateAndStart(RebuildIndexThreadProc, threadParam, threadCount);
                ptThreadCreation?.StopAndAccumulate();

                foreach (var documentPointer in documentPointers)
                {
                    if (threadPool.HasException || threadPool.ContinueToProcessQueue == false)
                    {
                        break;
                    }

                    threadPool.EnqueueWorkItem(documentPointer);
                }

                var ptThreadCompletion = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCompletion);
                threadPool.WaitForCompletion();
                ptThreadCompletion?.StopAndAccumulate();

                core.IO.PutPBuf(transaction, physicalIindex.DiskPath, physicalIndexPages);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to rebuild single index for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Removes a collection of document from all of the indexes on the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documentPointer"></param>
        internal void DeleteDocumentsFromIndexes(Transaction transaction, PhysicalSchema physicalSchema, IEnumerable<DocumentPointer> documentPointers)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    DeleteDocumentsFromIndex(transaction, physicalIindex, documentPointers);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Multi-index upsert failed for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private bool RemoveDocumentsFromLeaves(PhysicalIndexLeaf leaves, IEnumerable<DocumentPointer> documentPointers)
        {
            int deletes = 0;
            int neededDeletes = documentPointers.Count();
            foreach (var documentPointer in documentPointers)
            {
                if (leaves.Documents?.Count > 0)
                {
                    if (leaves.Documents.RemoveAll(o => o.PageNumber == documentPointer.PageNumber && o.DocumentId == documentPointer.DocumentId) > 0)
                    {
                        deletes++;
                    }
                }

                if (deletes == neededDeletes)
                {
                    return true; //We found the documents and removed them.
                }

                foreach (var leaf in leaves.Children)
                {
                    if (leaves.Children?.Count > 0)
                    {
                        if (RemoveDocumentsFromLeaves(leaf.Value, documentPointers))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes a collection of documents from an index. Locks the index page catalog for write.
        /// </summary>
        private void DeleteDocumentsFromIndex(Transaction transaction, PhysicalIndex physicalIindex, IEnumerable<DocumentPointer> documentPointers)
        {
            try
            {
                var physicalIndexPages = core.IO.GetPBuf<PhysicalIndexPages>(transaction, physicalIindex.DiskPath, LockOperation.Write);

                if (RemoveDocumentsFromLeaves(physicalIndexPages.Root, documentPointers))
                {
                    core.IO.PutPBuf(transaction, physicalIindex.DiskPath, physicalIndexPages);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Index document upsert failed for process {transaction.ProcessId}.", ex);
                throw;
            }
        }
    }
}
