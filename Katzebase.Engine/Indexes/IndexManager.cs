using Katzebase.Engine.Documents;
using Katzebase.Engine.Indexes.Matching;
using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Threading;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json.Linq;
using static Katzebase.Engine.Indexes.Matching.IndexConstants;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Indexes
{
    /// <summary>
    /// This is the class that all API controllers should interface with for index access.
    /// </summary>
    public class IndexManager
    {
        private Core core;
        public IndexManager(Core core)
        {
            this.core = core;
        }

        #region Query Handlers.

        internal KbActionResponse ExecuteDrop(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbActionResponse();
                var session = core.Sessions.ByProcessId(processId);

                using (var txRef = core.Transactions.Begin(processId))
                {
                    string schema = preparedQuery.Schemas.First().Name;

                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schema, LockOperation.Read);

                    Drop(txRef.Transaction, physicalSchema, preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.IndexName));

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

        internal KbActionResponse ExecuteRebuild(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbActionResponse();
                var session = core.Sessions.ByProcessId(processId);

                using (var txRef = core.Transactions.Begin(processId))
                {
                    string schema = preparedQuery.Schemas.First().Name;

                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schema, LockOperation.Read);

                    Rebuild(txRef.Transaction, physicalSchema, preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.IndexName));

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

        internal KbActionResponse ExecuteCreate(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbActionResponse();

                using (var txRef = core.Transactions.Begin(processId))
                {
                    string schema = preparedQuery.Schemas.First().Name;

                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schema, LockOperation.Read);

                    var index = new KbIndex
                    {
                        Name = preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.IndexName),
                        IsUnique = preparedQuery.Attribute<bool>(PreparedQuery.QueryAttribute.IsUnique)
                    };

                    foreach (var field in preparedQuery.SelectFields)
                    {
                        index.Attributes.Add(new KbIndexAttribute() { Field = field.Field });
                    }

                    Create(txRef.Transaction, physicalSchema, index, out Guid indexId);

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

        #endregion

        #region API Handlers.

        public List<KbIndex> GetList(ulong processId, string schema)
        {
            var result = new List<KbIndex>();
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schema, LockOperation.Read);
                    var indexCatalog = GetIndexCatalog(txRef.Transaction, physicalSchema, LockOperation.Read);
                    if (indexCatalog != null)
                    {
                        foreach (var index in indexCatalog.Collection)
                        {
                            result.Add(PhysicalIndex.ToPayload(index));
                        }
                    }


                    txRef.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to list indexes for process {processId}.", ex);
                throw;
            }

            return result;
        }

        public bool Exists(ulong processId, string schema, string indexName)
        {
            bool result = false;
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schema, LockOperation.Read);
                    var indexCatalog = GetIndexCatalog(txRef.Transaction, physicalSchema, LockOperation.Read);
                    if (indexCatalog != null)
                    {
                        result = indexCatalog.GetByName(indexName) != null;
                    }

                    txRef.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }

            return result;
        }

        private void Create(Transaction transaction, PhysicalSchema physicalSchema, KbIndex index, out Guid newId)
        {
            var physicalIndex = PhysicalIndex.FromPayload(index);

            physicalIndex.Id = Guid.NewGuid();
            physicalIndex.Created = DateTime.UtcNow;
            physicalIndex.Modfied = DateTime.UtcNow;

            var indexCatalog = GetIndexCatalog(transaction, physicalSchema, LockOperation.Write);

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

            newId = (Guid)physicalIndex.Id;
        }

        public void Create(ulong processId, string schema, KbIndex index, out Guid newId)
        {
            try
            {
                var physicalIndex = PhysicalIndex.FromPayload(index);

                using (var txRef = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schema, LockOperation.Read);
                    Create(txRef.Transaction, physicalSchema, index, out newId);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        public void Rebuild(ulong processId, string schema, string indexName)
        {
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schema, LockOperation.Read);
                    Rebuild(txRef.Transaction, physicalSchema, indexName);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        private void Drop(Transaction transaction, PhysicalSchema physicalSchema, string indexName)
        {
            var indexCatalog = GetIndexCatalog(transaction, physicalSchema, LockOperation.Write);
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

        private void Rebuild(Transaction transaction, PhysicalSchema physicalSchema, string indexName)
        {
            var indexCatalog = GetIndexCatalog(transaction, physicalSchema, LockOperation.Write);

            if (indexCatalog.DiskPath == null || physicalSchema.DiskPath == null)
            {
                throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
            }

            var physicalIindex = indexCatalog.GetByName(indexName) ?? throw new KbObjectNotFoundException(indexName);
            physicalIindex.DiskPath = Path.Combine(physicalSchema.DiskPath, MakeIndexFileName(physicalIindex.Name));

            RebuildIndex(transaction, physicalSchema, physicalIindex);
        }

        #endregion

        #region Core methods.

        internal Dictionary<Guid, PageDocument> MatchDocuments(Transaction transaction, PhysicalIndexPages physicalIndexPages,
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
                    return lastFoundPhysicalIndexLeaf.Documents.ToDictionary(o => o.Id, o => new PageDocument(o.Id, o.PageNumber));
                }
                else
                {
                    workingPhysicalIndexLeaf = lastFoundPhysicalIndexLeaf;
                }
            }

            if (foundAnything == false)
            {
                return new Dictionary<Guid, PageDocument>();
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
        internal Dictionary<Guid, PageDocument> MatchDocuments(Transaction transaction, PhysicalIndexPages physicalIndexPages, IndexSelection indexSelection, ConditionSubset conditionSubset)
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
                    return lastFoundPhysicalIndexLeaf.Documents.ToDictionary(o => o.Id, o => new PageDocument(o.Id, o.PageNumber));
                }
                else
                {
                    workingPhysicalIndexLeaf = lastFoundPhysicalIndexLeaf;
                }
            }

            if (foundAnything == false)
            {
                return new Dictionary<Guid, PageDocument>();
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
        private Dictionary<Guid, PageDocument> DistillIndexLeaves(PhysicalIndexLeaf physicalIndexLeaf)
        {
            var result = new List<PageDocument>();

            void DistillIndexLeavesRecursive(PhysicalIndexLeaf physicalIndexLeaf)
            {
                foreach (var child in physicalIndexLeaf.Children)
                {
                    DistillIndexLeavesRecursive(child.Value);
                }

                if (physicalIndexLeaf?.Documents?.Any() == true)
                {
                    result.AddRange(physicalIndexLeaf.Documents.Select(o => new PageDocument(o.Id, o.PageNumber)));
                }
            }

            if (physicalIndexLeaf?.Documents?.Any() == true)
            {
                result.AddRange(physicalIndexLeaf.Documents.Select(o => new PageDocument(o.Id, o.PageNumber)));
            }
            else if (physicalIndexLeaf?.Children != null)
            {
                foreach (var child in physicalIndexLeaf.Children)
                {
                    DistillIndexLeavesRecursive(child.Value);
                }
            }

            return result.ToDictionary(o => o.Id, o => new PageDocument(o.Id, o.PageNumber));
        }

        private PhysicalIndexCatalog GetIndexCatalog(Transaction transaction, string schema, LockOperation intendedOperation)
        {
            var physicalSchema = core.Schemas.Acquire(transaction, schema, intendedOperation);
            return GetIndexCatalog(transaction, physicalSchema, intendedOperation);
        }

        public string MakeIndexFileName(string indexName)
        {
            return $"@Index_{0}_Pages_{Helpers.MakeSafeFileName(indexName)}.PBuf";
        }

        internal PhysicalIndexCatalog GetIndexCatalog(Transaction transaction, PhysicalSchema physicalSchema, LockOperation intendedOperation)
        {
            if (physicalSchema.DiskPath == null)
            {
                throw new KbNullException($"Value should not be null {nameof(physicalSchema.DiskPath)}.");
            }

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
        private void UpdateDocumentIntoIndexes(Transaction transaction, PhysicalSchema physicalSchema, PhysicalDocument physicalDocument, PageDocument pageDocument)
        {
            try
            {
                var indexCatalog = GetIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    DeleteDocumentFromIndex(transaction, physicalSchema, physicalIindex, pageDocument);
                    InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, physicalDocument, pageDocument);
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
        internal void InsertDocumentIntoIndexes(Transaction transaction, PhysicalSchema physicalSchema, PhysicalDocument physicalDocument, PageDocument pageDocument)
        {
            try
            {
                var indexCatalog = GetIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, physicalDocument, pageDocument);
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
            PhysicalIndex physicalIindex, PhysicalDocument document, PageDocument pageDocument)
        {
            var physicalIndexPages = core.IO.GetPBuf<PhysicalIndexPages>(transaction, physicalIindex.DiskPath, LockOperation.Write);
            InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, document, pageDocument, physicalIndexPages, true);
        }

        /// <summary>
        /// Inserts an index entry for a single document into a single index using a long lived index page catalog.
        /// </summary>
        private void InsertDocumentIntoIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIindex,
            PhysicalDocument document, PageDocument pageDocument, PhysicalIndexPages physicalIndexPages, bool flushPageCatalog)
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
                indexScanResult.Leaf.Documents.Add(new PhysicalIndexEntry(pageDocument.Id, pageDocument.PageNumber));

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

        private void RebuildIndexThreadProc(ThreadPoolQueue<PageDocument, RebuildIndexThreadParam> pool, RebuildIndexThreadParam? param)
        {
            Utility.EnsureNotNull(param);

            while (pool.ContinueToProcessQueue)
            {
                var pageDocument = pool.DequeueWorkItem();
                if (pageDocument == null)
                {
                    continue;
                }

                if (param.PhysicalSchema.DiskPath == null)
                {
                    throw new KbNullException($"Value should not be null {nameof(param.PhysicalSchema.DiskPath)}.");
                }

                var PhysicalDocument = core.Documents.GetDocument(param.Transaction, param.PhysicalSchema, pageDocument.Id, LockOperation.Read);

                lock (param.SyncObject)
                {
                    InsertDocumentIntoIndex(param.Transaction, param.PhysicalSchema, param.PhysicalIindex, PhysicalDocument, pageDocument, param.PhysicalIndexPages, false);
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
                var documentCatalog = core.Documents.GetPageDocuments(transaction, physicalSchema, LockOperation.Read).ToList();

                //Clear out the existing index pages.
                core.IO.PutPBuf(transaction, physicalIindex.DiskPath, new PhysicalIndexPages());

                var physicalIndexPages = core.IO.GetPBuf<PhysicalIndexPages>(transaction, physicalIindex.DiskPath, LockOperation.Write);

                var ptThreadCreation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCreation);
                var threadParam = new RebuildIndexThreadParam(transaction, physicalSchema, physicalIndexPages, physicalIindex);
                int threadCount = ThreadPoolHelper.CalculateThreadCount(core.Sessions.ByProcessId(transaction.ProcessId), documentCatalog.Count);
                transaction.PT?.AddDescreteMetric(PerformanceTraceDescreteMetricType.ThreadCount, threadCount);
                var threadPool = ThreadPoolQueue<PageDocument, RebuildIndexThreadParam>
                    .CreateAndStart(RebuildIndexThreadProc, threadParam, threadCount);
                ptThreadCreation?.StopAndAccumulate();

                foreach (var pageDocument in documentCatalog)
                {
                    if (threadPool.HasException || threadPool.ContinueToProcessQueue == false)
                    {
                        break;
                    }

                    threadPool.EnqueueWorkItem(pageDocument);
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

        internal void DeleteDocumentFromIndexes(Transaction transaction, PhysicalSchema physicalSchema, PageDocument pageDocument)
        {
            try
            {
                var indexCatalog = GetIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    DeleteDocumentFromIndex(transaction, physicalSchema, physicalIindex, pageDocument);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Multi-index upsert failed for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private bool RemoveDocumentFromLeaves(PhysicalIndexLeaf leaves, Guid documentId)
        {
            if (leaves.Documents?.Count > 0)
            {
                if (leaves.Documents.RemoveAll(o => o.Id == documentId) > 0)
                {
                    return true; //We found the document and removed it.
                }
            }

            foreach (var leaf in leaves.Children)
            {
                if (leaves.Children?.Count > 0)
                {
                    if (RemoveDocumentFromLeaves(leaf.Value, documentId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes a document from an index. Locks the index page catalog for write.
        /// </summary>
        private void DeleteDocumentFromIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIindex, PageDocument pageDocument)
        {
            try
            {
                var physicalIndexPages = core.IO.GetPBuf<PhysicalIndexPages>(transaction, physicalIindex.DiskPath, LockOperation.Write);

                //TODO: We migth be able to work the page number into this:
                if (RemoveDocumentFromLeaves(physicalIndexPages.Root, pageDocument.Id))
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

        #endregion
    }
}
