using Katzebase.Engine.Documents;
using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json.Linq;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Indexes
{
    /// <summary>
    /// This is the class that all API controllers should interface with for index access.
    /// </summary>
    public class PersistIndexManager
    {
        private Core core;
        public PersistIndexManager(Core core)
        {
            this.core = core;
        }

        internal KbActionResponse ExecuteRebuild(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbActionResponse();

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

                    string schema = preparedQuery.Schemas.First().Name;

                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(preparedQuery.Schemas[0].Prefix);
                    }

                    Rebuild(txRef.Transaction, schemaMeta, preparedQuery.SubQueryObject);

                    txRef.Commit();

                    result.WaitTimes = txRef.Transaction.PT?.ToWaitTimes();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSelect for process {processId}.", ex);
                throw;
            }
        }

        internal HashSet<Guid> MatchDocuments(Transaction transaction, PersistIndexPageCatalog indexPageCatalog,
            IndexSelection indexSelection, ConditionSubset conditionSubset, Dictionary<string, string> conditionValues)
        {
            var indexEntires = indexPageCatalog.Leaves.Entries; //Start at the top of the index tree.

            bool? fullMatch = null;

            foreach (var attribute in indexSelection.Index.Attributes)
            {
                Utility.EnsureNotNull(attribute.Field);
                var conditionField = conditionSubset.Conditions.Where(o => o.Left.Value == attribute.Field.ToLowerInvariant()).FirstOrDefault();
                if (conditionField == null)
                {
                    //No match? I think this is an exception....
                    fullMatch = false;
                    break;
                }

                if (conditionField.LogicalConnector == LogicalConnector.Or)
                {
                    //TODO: Indexing only supports AND connectors, thats a performance problem.

                    //If we got here then we didnt get a full match and will need to add all of the child-leaf document IDs for later elimination.
                    var ptIndexDistillation = transaction.PT?.BeginTrace(PerformanceTrace.PerformanceTraceType.IndexDistillation);
                    var resultintDocuments = DistillIndexLeaves(indexEntires);
                    ptIndexDistillation?.EndTrace();
                    return resultintDocuments.ToHashSet();
                }

                List<PersistIndexLeaf>? nextIndexEntires = null;

                var ptIndexSeek = transaction.PT?.BeginTrace(PerformanceTrace.PerformanceTraceType.IndexSeek);

                var conditionValue = conditionValues[attribute.Field.ToLower()];

                if (conditionField.LogicalQualifier == LogicalQualifier.Equals)
                    nextIndexEntires = indexEntires.Where(o => o.Value == conditionValue)?.ToList();
                else if (conditionField.LogicalQualifier == LogicalQualifier.NotEquals)
                    nextIndexEntires = indexEntires.Where(o => o.Value != conditionValue)?.ToList();
                else throw new KbNotImplementedException($"Condition qualifier {conditionField.LogicalQualifier} has not been implemented.");

                ptIndexSeek?.EndTrace();

                if (nextIndexEntires == null)
                {
                    fullMatch = false; //No match, bail out!
                    break;
                }
                else
                {
                    fullMatch ??= true; //Set this as a FULL INDEX match on the first match. This is true until we fail to match on a subsequent condition.
                }

                if (nextIndexEntires.Any(o => o.Leaves.Count > 0)) //If we are at the base of the tree then there is no need to go further down.
                {
                    indexEntires = nextIndexEntires.Select(o => o.Leaves).SelectMany(o => o.Entries).ToList(); //Traverse down the tree.
                }
                else
                {
                    indexEntires = nextIndexEntires;
                }
            }

            if (fullMatch == true)
            {
                var ptIndexDistillation = transaction.PT?.BeginTrace(PerformanceTrace.PerformanceTraceType.IndexDistillation);
                var resultintDocuments = indexEntires.SelectMany(o => o.DocumentIDs ?? new HashSet<Guid>()).ToList();
                //If we got here then we got a full match on the entire index. This is the best scenario.
                ptIndexDistillation?.EndTrace();
                return resultintDocuments.ToHashSet();
            }
            else
            {
                var ptIndexDistillation = transaction.PT?.BeginTrace(PerformanceTrace.PerformanceTraceType.IndexDistillation);
                var resultintDocuments = DistillIndexLeaves(indexEntires);
                ptIndexDistillation?.EndTrace();
                //If we got here then we didnt get a full match and will need to add all of the child-leaf document IDs for later elimination.
                return resultintDocuments.ToHashSet();
            }
        }

        /// <summary>
        /// Finds document IDs given a set of conditions.
        /// </summary>
        internal HashSet<Guid> MatchDocuments(Transaction transaction, PersistIndexPageCatalog indexPageCatalog, IndexSelection indexSelection, ConditionSubset conditionSubset)
        {
            var indexEntires = indexPageCatalog.Leaves.Entries; //Start at the top of the index tree.

            bool? fullMatch = null;

            foreach (var attribute in indexSelection.Index.Attributes)
            {
                Utility.EnsureNotNull(attribute.Field);
                var conditionField = conditionSubset.Conditions.Where(o => o.Left.Value == attribute.Field.ToLowerInvariant()).FirstOrDefault();
                if (conditionField == null)
                {
                    //No match? I think this is an exception....
                    fullMatch = false;
                    break;
                }

                if (conditionField.LogicalConnector == LogicalConnector.Or)
                {
                    //TODO: Indexing only supports AND connectors, thats a performance problem.

                    //If we got here then we didnt get a full match and will need to add all of the child-leaf document IDs for later elimination.
                    var ptIndexDistillation = transaction.PT?.BeginTrace(PerformanceTrace.PerformanceTraceType.IndexDistillation);
                    var resultintDocuments = DistillIndexLeaves(indexEntires);
                    ptIndexDistillation?.EndTrace();
                    return resultintDocuments.ToHashSet();
                }

                List<PersistIndexLeaf>? nextIndexEntires = null;

                var ptIndexSeek = transaction.PT?.BeginTrace(PerformanceTrace.PerformanceTraceType.IndexSeek);

                if (conditionField.LogicalQualifier == LogicalQualifier.Equals)
                    nextIndexEntires = indexEntires.Where(o => o.Value == conditionField.Right.Value)?.ToList();
                else if (conditionField.LogicalQualifier == LogicalQualifier.NotEquals)
                    nextIndexEntires = indexEntires.Where(o => o.Value != conditionField.Right.Value)?.ToList();
                else throw new KbNotImplementedException($"Condition qualifier {conditionField.LogicalQualifier} has not been implemented.");

                ptIndexSeek?.EndTrace();

                if (nextIndexEntires == null)
                {
                    fullMatch = false; //No match, bail out!
                    break;
                }
                else
                {
                    fullMatch ??= true; //Set this as a FULL INDEX match on the first match. This is true until we fail to match on a subsequent condition.
                }

                if (indexEntires.Any(o => o.Leaves.Count > 0)) //If we are at the base of the tree then there is no need to go further down.
                {
                    indexEntires = nextIndexEntires.Select(o => o.Leaves).SelectMany(o => o.Entries).ToList(); //Traverse down the tree.
                }
                else
                {
                    indexEntires = nextIndexEntires;
                }
            }

            if (fullMatch == true)
            {
                var ptIndexDistillation = transaction.PT?.BeginTrace(PerformanceTrace.PerformanceTraceType.IndexDistillation);
                var resultintDocuments = indexEntires.SelectMany(o => o.DocumentIDs ?? new HashSet<Guid>()).ToList();
                //If we got here then we got a full match on the entire index. This is the best scenario.
                ptIndexDistillation?.EndTrace();
                return resultintDocuments.ToHashSet();
            }
            else
            {
                var ptIndexDistillation = transaction.PT?.BeginTrace(PerformanceTrace.PerformanceTraceType.IndexDistillation);
                var resultintDocuments = DistillIndexLeaves(indexEntires);
                ptIndexDistillation?.EndTrace();
                //If we got here then we didnt get a full match and will need to add all of the child-leaf document IDs for later elimination.
                return resultintDocuments.ToHashSet();
            }
        }

        /// <summary>
        /// Traverse to the bottom of the index tree (what whatever starting point is passed in) and return a list of all documentids.
        /// </summary>
        /// <param name="indexEntires"></param>
        /// <returns></returns>
        private List<Guid> DistillIndexLeaves(List<PersistIndexLeaf> indexEntires)
        {
            while (indexEntires.Any(o => o.Leaves.Count > 0))
            {
                indexEntires = indexEntires.Select(o => o.Leaves).SelectMany(o => o.Entries).ToList(); //Traverse down the tree.
            }

            return indexEntires.SelectMany(o => o.DocumentIDs ?? new HashSet<Guid>()).ToList();
        }

        public List<KbIndex> GetList(ulong processId, string schema)
        {
            var result = new List<KbIndex>();
            try
            {
                using (var txRef = core.Transactions.Begin(processId))
                {
                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(schema);
                    }

                    var indexCatalog = GetIndexCatalog(txRef.Transaction, schemaMeta, LockOperation.Write);
                    if (indexCatalog != null)
                    {
                        foreach (var index in indexCatalog.Collection)
                        {
                            result.Add(PersistIndex.ToPayload(index));
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
                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(schema);
                    }

                    var indexCatalog = GetIndexCatalog(txRef.Transaction, schemaMeta, LockOperation.Write);
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

        public void Create(ulong processId, string schema, KbIndex index, out Guid newId)
        {
            try
            {
                var persistIndex = PersistIndex.FromPayload(index);
                Utility.EnsureNotNull(persistIndex);

                if (persistIndex.Id == null || persistIndex.Id == Guid.Empty)
                {
                    persistIndex.Id = Guid.NewGuid();
                }
                if (persistIndex.Created == DateTime.MinValue)
                {
                    persistIndex.Created = DateTime.UtcNow;
                }
                if (persistIndex.Modfied == DateTime.MinValue)
                {
                    persistIndex.Modfied = DateTime.UtcNow;
                }

                using (var txRef = core.Transactions.Begin(processId))
                {
                    var schemaMeta = core.Schemas.VirtualPathToMeta(txRef.Transaction, schema, LockOperation.Read);
                    if (schemaMeta == null || schemaMeta.Exists == false)
                    {
                        throw new KbInvalidSchemaException(schema);
                    }

                    var indexCatalog = GetIndexCatalog(txRef.Transaction, schemaMeta, LockOperation.Write);
                    indexCatalog.Add(persistIndex);

                    if (indexCatalog.DiskPath == null || schemaMeta.DiskPath == null)
                    {
                        throw new KbNullException($"Value should not be null {nameof(schemaMeta.DiskPath)}.");
                    }

                    core.IO.PutJson(txRef.Transaction, indexCatalog.DiskPath, indexCatalog);
                    persistIndex.DiskPath = Path.Combine(schemaMeta.DiskPath, MakeIndexFileName(index.Name));
                    core.IO.PutPBuf(txRef.Transaction, persistIndex.DiskPath, new PersistIndexPageCatalog());

                    RebuildIndex(txRef.Transaction, schemaMeta, persistIndex);

                    newId = (Guid)persistIndex.Id;

                    txRef.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        internal void Rebuild(Transaction transaction, PersistSchema schemaMeta, string indexName)
        {
            var indexCatalog = GetIndexCatalog(transaction, schemaMeta, LockOperation.Write);

            if (indexCatalog.DiskPath == null || schemaMeta.DiskPath == null)
            {
                throw new KbNullException($"Value should not be null {nameof(schemaMeta.DiskPath)}.");
            }

            var indexMeta = indexCatalog.GetByName(indexName);
            if (indexMeta == null)
            {
                throw new KbIndexDoesNotExistException(indexName);
            }

            indexMeta.DiskPath = Path.Combine(schemaMeta.DiskPath, MakeIndexFileName(indexMeta.Name));

            RebuildIndex(transaction, schemaMeta, indexMeta);
        }

        internal PersistIndexCatalog GetIndexCatalog(Transaction transaction, string schema, LockOperation intendedOperation)
        {
            var schemaMeta = core.Schemas.VirtualPathToMeta(transaction, schema, intendedOperation);
            return GetIndexCatalog(transaction, schemaMeta, intendedOperation);
        }

        public string MakeIndexFileName(string indexName)
        {
            return $"@Index_{0}_Pages_{Helpers.MakeSafeFileName(indexName)}.PBuf";
        }

        internal PersistIndexCatalog GetIndexCatalog(Transaction transaction, PersistSchema schemaMeta, LockOperation intendedOperation)
        {
            if (schemaMeta.DiskPath == null)
            {
                throw new KbNullException($"Value should not be null {nameof(schemaMeta.DiskPath)}.");
            }

            string indexCatalogDiskPath = Path.Combine(schemaMeta.DiskPath, IndexCatalogFile);

            var indexCatalog = core.IO.GetJson<PersistIndexCatalog>(transaction, indexCatalogDiskPath, intendedOperation);
            Utility.EnsureNotNull(indexCatalog);

            indexCatalog.DiskPath = indexCatalogDiskPath;

            foreach (var index in indexCatalog.Collection)
            {
                index.DiskPath = Path.Combine(schemaMeta.DiskPath, MakeIndexFileName(index.Name));
            }

            return indexCatalog;
        }

        private List<string> GetIndexSearchTokens(Transaction transaction, PersistIndex indexMeta, PersistDocument document)
        {
            try
            {
                List<string> result = new List<string>();

                Utility.EnsureNotNull(document.Content);

                foreach (var indexAttribute in indexMeta.Attributes)
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
        /// Finds the appropriate index page for a set of key values in the given index file.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="indexMeta"></param>
        /// <param name="searchTokens"></param>
        /// <returns></returns>
        private FindKeyPageResult LocateExtentInGivenIndexFile(Transaction transaction, List<string> searchTokens, PersistIndex indexMeta)
        {
            Utility.EnsureNotNull(indexMeta.DiskPath);
            var indexPageCatalog = core.IO.GetPBuf<PersistIndexPageCatalog>(transaction, indexMeta.DiskPath, LockOperation.Write);
            Utility.EnsureNotNull(indexPageCatalog);
            return LocateExtentInGivenIndexPageCatalog(transaction, searchTokens, indexPageCatalog);
        }

        /// <summary>
        /// Finds the appropriate index page for a set of key values in the given index page catalog.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="indexMeta"></param>
        /// <param name="searchTokens"></param>
        /// <param name="indexPageCatalog"></param>
        /// <returns>A reference to a node in the suppliedIndexPageCatalog</returns>
        private FindKeyPageResult LocateExtentInGivenIndexPageCatalog(Transaction transaction, List<string> searchTokens, PersistIndexPageCatalog suppliedIndexPageCatalog)
        {
            try
            {
                var indexPageCatalog = suppliedIndexPageCatalog;

                Utility.EnsureNotNull(indexPageCatalog);

                lock (indexPageCatalog)
                {
                    FindKeyPageResult result = new FindKeyPageResult()
                    {
                        Catalog = indexPageCatalog
                    };

                    result.Leaves = result.Catalog.Leaves;
                    Utility.EnsureNotNull(result.Leaves);

                    if (result.Leaves.Count == 0)
                    {
                        //The index is empty.
                        return result;
                    }

                    int foundExtentCount = 0;

                    foreach (var token in searchTokens)
                    {
                        bool locatedExtent = false;

                        foreach (var leaf in result.Leaves)
                        {
                            if (leaf.Value == token)
                            {
                                locatedExtent = true;
                                foundExtentCount++;
                                result.Leaf = leaf;
                                result.Leaves = leaf.Leaves; //Move one level lower in the extent tree.

                                result.IsPartialMatch = true;
                                result.ExtentLevel = foundExtentCount;

                                if (foundExtentCount == searchTokens.Count)
                                {
                                    result.IsPartialMatch = false;
                                    result.IsFullMatch = true;
                                    return result;
                                }
                            }
                        }

                        if (locatedExtent == false)
                        {
                            return result;
                        }
                    }

                    return result;
                }
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
        internal void UpdateDocumentIntoIndexes(Transaction transaction, PersistSchema schemaMeta, PersistDocument document)
        {
            try
            {
                Utility.EnsureNotNull(document.Id);

                var indexCatalog = GetIndexCatalog(transaction, schemaMeta, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var indexMeta in indexCatalog.Collection)
                {
                    DeleteDocumentFromIndex(transaction, schemaMeta, indexMeta, (Guid)document.Id);
                    InsertDocumentIntoIndex(transaction, schemaMeta, indexMeta, document);
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
        internal void InsertDocumentIntoIndexes(Transaction transaction, PersistSchema schemaMeta, PersistDocument document)
        {
            try
            {
                var indexCatalog = GetIndexCatalog(transaction, schemaMeta, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var indexMeta in indexCatalog.Collection)
                {
                    InsertDocumentIntoIndex(transaction, schemaMeta, indexMeta, document);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Multi-index insert failed for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts an index entry for a single document into a single index.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schemaMeta"></param>
        /// <param name="indexMeta"></param>
        /// <param name="document"></param>
        private void InsertDocumentIntoIndex(Transaction transaction, PersistSchema schemaMeta, PersistIndex indexMeta, PersistDocument document)
        {
            InsertDocumentIntoIndex(transaction, schemaMeta, indexMeta, document, null, true);
        }

        /// <summary>
        /// Inserts an index entry for a single document into a single index using a long lived index page catalog.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schemaMeta"></param>
        /// <param name="indexMeta"></param>
        /// <param name="document"></param>
        private void InsertDocumentIntoIndex(Transaction transaction, PersistSchema schemaMeta, PersistIndex indexMeta, PersistDocument document, PersistIndexPageCatalog? indexPageCatalog, bool flushPageCatalog)
        {
            try
            {
                Utility.EnsureNotNullOrEmpty(document.Id);
                Utility.EnsureNotNull(indexMeta.DiskPath);

                var searchTokens = GetIndexSearchTokens(transaction, indexMeta, document);

                FindKeyPageResult findResult;

                if (indexPageCatalog == null)
                {
                    findResult = LocateExtentInGivenIndexFile(transaction, searchTokens, indexMeta);
                }
                else
                {
                    findResult = LocateExtentInGivenIndexPageCatalog(transaction, searchTokens, indexPageCatalog);
                }

                Utility.EnsureNotNull(findResult.Catalog);

                if (findResult.IsFullMatch) //If we found a full match for all supplied key values - add the document to the leaf collection.
                {
                    Utility.EnsureNotNull(findResult.Leaf);

                    if (findResult.Leaf.DocumentIDs == null)
                    {
                        findResult.Leaf.DocumentIDs = new HashSet<Guid>();
                    }

                    if (indexMeta.IsUnique && findResult.Leaf.DocumentIDs.Count > 1)
                    {
                        string exceptionText = $"Duplicate key violation occurred for index [{schemaMeta.VirtualPath}]/[{indexMeta.Name}]. Values: {{{string.Join(",", searchTokens)}}}";
                        throw new KbDuplicateKeyViolationException(exceptionText);
                    }

                    findResult.Leaf.DocumentIDs.Add((Guid)document.Id);
                    if (flushPageCatalog)
                    {
                        core.IO.PutPBuf(transaction, indexMeta.DiskPath, findResult.Catalog);
                    }
                }
                else
                {
                    //If we didn't find a full match for all supplied key values,
                    //  then create the tree and add the document to the lowest leaf.
                    //Note that we are going to start creating the leaf level at the findResult.ExtentLevel.
                    //  This is because we may have a partial match and don't need to create the full tree.

                    Utility.EnsureNotNull(indexPageCatalog);
                    Utility.EnsureNotNull(findResult.Leaves);

                    lock (indexPageCatalog)
                    {
                        for (int i = findResult.ExtentLevel; i < searchTokens.Count; i++)
                        {
                            findResult.Leaf = findResult.Leaves.AddNewleaf(searchTokens[i]);
                            findResult.Leaves = findResult.Leaf.Leaves;
                        }

                        Utility.EnsureNotNull(findResult.Leaf);

                        if (findResult.Leaf.DocumentIDs == null)
                        {
                            findResult.Leaf.DocumentIDs = new HashSet<Guid>();
                        }

                        findResult.Leaf.DocumentIDs.Add((Guid)document.Id);
                    }

                    //Utility.AssertIfDebug(findResult.Catalog.Leaves.Entries.GroupBy(o => o.Key).Where(o => o.Count() > 1).Any(), "Duplicate root index entry.");

                    if (flushPageCatalog)
                    {
                        core.IO.PutPBuf(transaction, indexMeta.DiskPath, findResult.Catalog);
                    }
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Index document insert failed for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal class RebuildIndexItemThreadProc_ParallelState
        {
            public int ThreadsCompleted { get; set; }
            public int ThreadsStarted { get; set; }
            public int TargetThreadCount { get; set; }

            public bool IsComplete
            {
                get
                {
                    lock (this)
                    {
                        return (ThreadsStarted - ThreadsCompleted) == 0;
                    }
                }
            }
        }

        internal class RebuildIndexItemThreadProc_Params
        {
            public RebuildIndexItemThreadProc_ParallelState? State { get; set; }
            public Transaction? Transaction { get; set; }
            public PersistSchema? SchemaMeta { get; set; }
            public PersistIndex? IndexMeta { get; set; }
            public PersistDocumentCatalog? DocumentCatalog { get; set; }
            public PersistIndexPageCatalog? IndexPageCatalog { get; set; }
            public AutoResetEvent Initialized { get; set; }
            public object SyncObject { get; set; } = new object();

            public RebuildIndexItemThreadProc_Params()
            {
                Initialized = new AutoResetEvent(false);
            }
        }

        internal void RebuildIndexItemThreadProc(object? oParam)
        {
            int threadMod = 0;

            Utility.EnsureNotNull(oParam);

            var param = (RebuildIndexItemThreadProc_Params)oParam;

            Utility.EnsureNotNull(param.State);
            Utility.EnsureNotNull(param.DocumentCatalog);
            Utility.EnsureNotNull(param.SchemaMeta);
            Utility.EnsureNotNull(param.Transaction);
            Utility.EnsureNotNull(param.IndexMeta);
            Utility.EnsureNotNull(param.IndexPageCatalog);

            lock (param.State)
            {
                threadMod = param.State.ThreadsStarted;
                param.State.ThreadsStarted++;
                Thread.CurrentThread.Name = "RebuildIndexItemThreadProc_" + param.State.ThreadsStarted;
                param.Initialized.Set();
            }

            for (int i = 0; i < param.DocumentCatalog.Collection.Count; i++)
            {
                if ((i % param.State.TargetThreadCount) == threadMod)
                {
                    var documentCatalogItem = param.DocumentCatalog.Collection[i];

                    if (param.SchemaMeta.DiskPath == null)
                    {
                        throw new KbNullException($"Value should not be null {nameof(param.SchemaMeta.DiskPath)}.");
                    }

                    string documentDiskPath = Path.Combine(param.SchemaMeta.DiskPath, documentCatalogItem.FileName);
                    var persistDocument = core.IO.GetJson<PersistDocument>(param.Transaction, documentDiskPath, LockOperation.Read);
                    Utility.EnsureNotNull(persistDocument);

                    lock (param.SyncObject)
                    {
                        InsertDocumentIntoIndex(param.Transaction, param.SchemaMeta, param.IndexMeta, persistDocument, param.IndexPageCatalog, false);
                    }
                }
            }

            lock (param.State)
            {
                param.State.ThreadsCompleted++;
            }
        }

        /// <summary>
        /// Inserts all documents in a schema into a single index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schemaMeta"></param>
        /// <param name="indexMeta"></param>
        private void RebuildIndex(Transaction transaction, PersistSchema schemaMeta, PersistIndex indexMeta)
        {
            try
            {
                Utility.EnsureNotNull(indexMeta.DiskPath);
                Utility.EnsureNotNull(schemaMeta.DiskPath);

                var filePath = Path.Combine(schemaMeta.DiskPath, DocumentCatalogFile);
                var documentCatalog = core.IO.GetJson<PersistDocumentCatalog>(transaction, filePath, LockOperation.Read);

                //Clear out the existing index pages.
                core.IO.PutPBuf(transaction, indexMeta.DiskPath, new PersistIndexPageCatalog());

                var indexPageCatalog = core.IO.GetPBuf<PersistIndexPageCatalog>(transaction, indexMeta.DiskPath, LockOperation.Write);

                var state = new RebuildIndexItemThreadProc_ParallelState()
                {
                    TargetThreadCount = Environment.ProcessorCount * 2
                };

                var param = new RebuildIndexItemThreadProc_Params()
                {
                    DocumentCatalog = documentCatalog,
                    State = state,
                    IndexMeta = indexMeta,
                    IndexPageCatalog = indexPageCatalog,
                    SchemaMeta = schemaMeta,
                    Transaction = transaction
                };

                for (int i = 0; i < state.TargetThreadCount; i++)
                {
                    new Thread(RebuildIndexItemThreadProc).Start(param);
                    param.Initialized.WaitOne(Timeout.Infinite);
                }

                while (state.IsComplete == false)
                {
                    Thread.Sleep(1);
                }

                Utility.EnsureNotNull(indexPageCatalog);

                core.IO.PutPBuf(transaction, indexMeta.DiskPath, indexPageCatalog);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to rebuild single index for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void DeleteDocumentFromIndexes(Transaction transaction, PersistSchema schemaMeta, Guid documentId)
        {
            try
            {
                var indexCatalog = GetIndexCatalog(transaction, schemaMeta, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var indexMeta in indexCatalog.Collection)
                {
                    DeleteDocumentFromIndex(transaction, schemaMeta, indexMeta, documentId);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Multi-index upsert failed for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private bool RemoveDocumentFromLeaves(ref PersistIndexLeaves leaves, Guid documentId)
        {
            foreach (var leaf in leaves)
            {
                if (leaf.DocumentIDs != null && leaf.DocumentIDs.Count > 0)
                {
                    if (leaf.DocumentIDs.Remove(documentId))
                    {
                        return true; //We found the document and removed it.
                    }
                }

                if (leaf.Leaves != null && leaf.Leaves.Count > 0)
                {
                    if (RemoveDocumentFromLeaves(ref leaf.Leaves, documentId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes a document from an index.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schemaMeta"></param>
        /// <param name="indexMeta"></param>
        /// <param name="document"></param>
        private void DeleteDocumentFromIndex(Transaction transaction, PersistSchema schemaMeta, PersistIndex indexMeta, Guid documentId)
        {
            try
            {
                Utility.EnsureNotNull(indexMeta.DiskPath);

                var persistIndexPageCatalog = core.IO.GetPBuf<PersistIndexPageCatalog>(transaction, indexMeta.DiskPath, LockOperation.Write);

                Utility.EnsureNotNull(persistIndexPageCatalog);

                if (RemoveDocumentFromLeaves(ref persistIndexPageCatalog.Leaves, documentId))
                {
                    core.IO.PutPBuf(transaction, indexMeta.DiskPath, persistIndexPageCatalog);
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
