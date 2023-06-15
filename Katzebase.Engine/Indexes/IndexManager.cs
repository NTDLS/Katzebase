using Katzebase.Engine.Documents;
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
                    if (physicalSchema?.Exists != true)
                    {
                        throw new KbObjectNotFoundException(preparedQuery.Schemas[0].Prefix);
                    }

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
                    if (physicalSchema?.Exists != true)
                    {
                        throw new KbObjectNotFoundException(preparedQuery.Schemas[0].Prefix);
                    }

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
                    if (physicalSchema?.Exists != true)
                    {
                        throw new KbObjectNotFoundException(preparedQuery.Schemas[0].Prefix);
                    }

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
                    if (physicalSchema?.Exists != true)
                    {
                        throw new KbObjectNotFoundException(schema);
                    }

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
                    if (physicalSchema?.Exists != true)
                    {
                        throw new KbObjectNotFoundException(schema);
                    }

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
            Utility.EnsureNotNull(physicalIndex);

            if (physicalIndex.Id == null || physicalIndex.Id == Guid.Empty)
            {
                physicalIndex.Id = Guid.NewGuid();
            }
            if (physicalIndex.Created == DateTime.MinValue)
            {
                physicalIndex.Created = DateTime.UtcNow;
            }
            if (physicalIndex.Modfied == DateTime.MinValue)
            {
                physicalIndex.Modfied = DateTime.UtcNow;
            }

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
            core.IO.PutPBuf(transaction, physicalIndex.DiskPath, new PhysicalIndexPageCatalog());

            RebuildIndex(transaction, physicalSchema, physicalIndex);

            newId = (Guid)physicalIndex.Id;
        }

        public void Create(ulong processId, string schema, KbIndex index, out Guid newId)
        {
            try
            {
                var physicalIndex = PhysicalIndex.FromPayload(index);
                Utility.EnsureNotNull(physicalIndex);

                using (var txRef = core.Transactions.Begin(processId))
                {
                    var physicalSchema = core.Schemas.Acquire(txRef.Transaction, schema, LockOperation.Read);
                    if (physicalSchema?.Exists != true)
                    {
                        throw new KbObjectNotFoundException(schema);
                    }

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
                    if (physicalSchema?.Exists != true)
                    {
                        throw new KbObjectNotFoundException(schema);
                    }

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

        internal HashSet<Guid> MatchDocuments(Transaction transaction, PhysicalIndexPageCatalog physicalIndexPageCatalog,
            IndexSelection indexSelection, ConditionSubset conditionSubset, Dictionary<string, string> conditionValues)
        {
            var indexEntires = physicalIndexPageCatalog.Leaves.Entries; //Start at the top of the index tree.

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
                    var ptIndexDistillation = transaction.PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.IndexDistillation);
                    var resultintDocuments = DistillIndexLeaves(indexEntires);
                    ptIndexDistillation?.StopAndAccumulate();
                    return resultintDocuments.ToHashSet();
                }

                List<PhysicalIndexLeaf>? nextPhysicalIndexEntires = null;

                var ptIndexSeek = transaction.PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.IndexSeek);

                var conditionValue = conditionValues[attribute.Field.ToLower()];

                if (conditionField.LogicalQualifier == LogicalQualifier.Equals)
                    nextPhysicalIndexEntires = indexEntires.Where(o => o.Value == conditionValue)?.ToList();
                else if (conditionField.LogicalQualifier == LogicalQualifier.NotEquals)
                    nextPhysicalIndexEntires = indexEntires.Where(o => o.Value != conditionValue)?.ToList();
                else throw new KbNotImplementedException($"Condition qualifier {conditionField.LogicalQualifier} has not been implemented.");

                ptIndexSeek?.StopAndAccumulate();

                if (nextPhysicalIndexEntires == null)
                {
                    fullMatch = false; //No match, bail out!
                    break;
                }
                else
                {
                    fullMatch ??= true; //Set this as a FULL INDEX match on the first match. This is true until we fail to match on a subsequent condition.
                }

                if (nextPhysicalIndexEntires.Any(o => o.Leaves.Count > 0)) //If we are at the base of the tree then there is no need to go further down.
                {
                    indexEntires = nextPhysicalIndexEntires.Select(o => o.Leaves).SelectMany(o => o.Entries).ToList(); //Traverse down the tree.
                }
                else
                {
                    indexEntires = nextPhysicalIndexEntires;
                }
            }

            if (fullMatch == true)
            {
                var ptIndexDistillation = transaction.PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.IndexDistillation);
                var resultintDocuments = indexEntires.SelectMany(o => o.DocumentIDs ?? new HashSet<Guid>()).ToList();
                //If we got here then we got a full match on the entire index. This is the best scenario.
                ptIndexDistillation?.StopAndAccumulate();
                return resultintDocuments.ToHashSet();
            }
            else
            {
                var ptIndexDistillation = transaction.PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.IndexDistillation);
                var resultintDocuments = DistillIndexLeaves(indexEntires);
                ptIndexDistillation?.StopAndAccumulate();
                //If we got here then we didnt get a full match and will need to add all of the child-leaf document IDs for later elimination.
                return resultintDocuments.ToHashSet();
            }
        }

        /// <summary>
        /// Finds document IDs given a set of conditions.
        /// </summary>
        internal HashSet<Guid> MatchDocuments(Transaction transaction, PhysicalIndexPageCatalog indexPageCatalog, IndexSelection indexSelection, ConditionSubset conditionSubset)
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
                    var ptIndexDistillation = transaction.PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.IndexDistillation);
                    var resultintDocuments = DistillIndexLeaves(indexEntires);
                    ptIndexDistillation?.StopAndAccumulate();
                    return resultintDocuments.ToHashSet();
                }

                List<PhysicalIndexLeaf>? nextPhysicalIndexEntires = null;

                var ptIndexSeek = transaction.PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.IndexSeek);

                if (conditionField.LogicalQualifier == LogicalQualifier.Equals)
                    nextPhysicalIndexEntires = indexEntires.Where(o => o.Value == conditionField.Right.Value)?.ToList();
                else if (conditionField.LogicalQualifier == LogicalQualifier.NotEquals)
                    nextPhysicalIndexEntires = indexEntires.Where(o => o.Value != conditionField.Right.Value)?.ToList();
                else throw new KbNotImplementedException($"Condition qualifier {conditionField.LogicalQualifier} has not been implemented.");

                ptIndexSeek?.StopAndAccumulate();

                if (nextPhysicalIndexEntires == null)
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
                    indexEntires = nextPhysicalIndexEntires.Select(o => o.Leaves).SelectMany(o => o.Entries).ToList(); //Traverse down the tree.
                }
                else
                {
                    indexEntires = nextPhysicalIndexEntires;
                }
            }

            if (fullMatch == true)
            {
                var ptIndexDistillation = transaction.PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.IndexDistillation);
                var resultintDocuments = indexEntires.SelectMany(o => o.DocumentIDs ?? new HashSet<Guid>()).ToList();
                //If we got here then we got a full match on the entire index. This is the best scenario.
                ptIndexDistillation?.StopAndAccumulate();
                return resultintDocuments.ToHashSet();
            }
            else
            {
                var ptIndexDistillation = transaction.PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.IndexDistillation);
                var resultintDocuments = DistillIndexLeaves(indexEntires);
                ptIndexDistillation?.StopAndAccumulate();
                //If we got here then we didnt get a full match and will need to add all of the child-leaf document IDs for later elimination.
                return resultintDocuments.ToHashSet();
            }
        }

        /// <summary>
        /// Traverse to the bottom of the index tree (what whatever starting point is passed in) and return a list of all documentids.
        /// </summary>
        /// <param name="indexEntires"></param>
        /// <returns></returns>
        private List<Guid> DistillIndexLeaves(List<PhysicalIndexLeaf> indexEntires)
        {
            while (indexEntires.Any(o => o.Leaves.Count > 0))
            {
                indexEntires = indexEntires.Select(o => o.Leaves).SelectMany(o => o.Entries).ToList(); //Traverse down the tree.
            }

            return indexEntires.SelectMany(o => o.DocumentIDs ?? new HashSet<Guid>()).ToList();
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
            Utility.EnsureNotNull(indexCatalog);

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
                List<string> result = new List<string>();

                Utility.EnsureNotNull(document.Content);

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
        /// Finds the appropriate index page for a set of key values in the given index file. Locks the index page catalog for write.
        /// </summary>
        /// <returns></returns>
        private FindKeyPageResult LocateExtentInGivenIndexFile(Transaction transaction, List<string> searchTokens, PhysicalIndex physicalIindex)
        {
            Utility.EnsureNotNull(physicalIindex.DiskPath);
            var indexPageCatalog = core.IO.GetPBuf<PhysicalIndexPageCatalog>(transaction, physicalIindex.DiskPath, LockOperation.Write);
            Utility.EnsureNotNull(indexPageCatalog);
            return LocateExtentInGivenIndexPageCatalog(transaction, searchTokens, indexPageCatalog);
        }

        /// <summary>
        /// Finds the appropriate index page for a set of key values in the given index page catalog.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalIindex"></param>
        /// <param name="searchTokens"></param>
        /// <param name="indexPageCatalog"></param>
        /// <returns>A reference to a node in the suppliedIndexPageCatalog</returns>
        private FindKeyPageResult LocateExtentInGivenIndexPageCatalog(Transaction transaction, List<string> searchTokens, PhysicalIndexPageCatalog suppliedIndexPageCatalog)
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

                        var matchingLeaf = result.Leaves.FirstOrDefault(o => o.Value == token);
                        if (matchingLeaf != null)
                        {
                            locatedExtent = true;
                            foundExtentCount++;
                            result.Leaf = matchingLeaf;
                            result.Leaves = matchingLeaf.Leaves; //Move one level lower in the extent tree.

                            result.IsPartialMatch = true;
                            result.ExtentLevel = foundExtentCount;

                            if (foundExtentCount == searchTokens.Count)
                            {
                                result.IsPartialMatch = false;
                                result.IsFullMatch = true;
                                return result;
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
        private void UpdateDocumentIntoIndexes(Transaction transaction, PhysicalSchema physicalSchema, PhysicalDocument document)
        {
            try
            {
                Utility.EnsureNotNull(document.Id);

                var indexCatalog = GetIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    DeleteDocumentFromIndex(transaction, physicalSchema, physicalIindex, (Guid)document.Id);
                    InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, document);
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
        internal void InsertDocumentIntoIndexes(Transaction transaction, PhysicalSchema physicalSchema, PhysicalDocument document)
        {
            try
            {
                var indexCatalog = GetIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, document);
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
        /// <param name="physicalSchema"></param>
        /// <param name="physicalIindex"></param>
        /// <param name="document"></param>
        private void InsertDocumentIntoIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIindex, PhysicalDocument document)
        {
            InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, document, null, true);
        }

        /// <summary>
        /// Inserts an index entry for a single document into a single index using a long lived index page catalog.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="physicalIindex"></param>
        /// <param name="document"></param>
        private void InsertDocumentIntoIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIindex, PhysicalDocument document, PhysicalIndexPageCatalog? indexPageCatalog, bool flushPageCatalog)
        {
            try
            {
                Utility.EnsureNotNullOrEmpty(document.Id);
                Utility.EnsureNotNull(physicalIindex.DiskPath);

                var searchTokens = GetIndexSearchTokens(transaction, physicalIindex, document);

                FindKeyPageResult findResult;

                if (indexPageCatalog == null)
                {
                    findResult = LocateExtentInGivenIndexFile(transaction, searchTokens, physicalIindex);
                }
                else
                {
                    findResult = LocateExtentInGivenIndexPageCatalog(transaction, searchTokens, indexPageCatalog);
                }

                Utility.EnsureNotNull(findResult.Catalog);

                if (findResult.IsFullMatch) //If we found a full match for all supplied key values - add the document to the leaf collection.
                {
                    Utility.EnsureNotNull(findResult.Leaf);

                    findResult.Leaf.DocumentIDs ??= new HashSet<Guid>();

                    if (physicalIindex.IsUnique && findResult.Leaf.DocumentIDs.Count > 1)
                    {
                        string exceptionText = $"Duplicate key violation occurred for index [{physicalSchema.VirtualPath}]/[{physicalIindex.Name}]. Values: {{{string.Join(",", searchTokens)}}}";
                        throw new KbDuplicateKeyViolationException(exceptionText);
                    }

                    findResult.Leaf.DocumentIDs.Add((Guid)document.Id);
                    if (flushPageCatalog)
                    {
                        core.IO.PutPBuf(transaction, physicalIindex.DiskPath, findResult.Catalog);
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

                    //Utility.AssertIfDebug(findResult.Catalog.Leaves.Entries.GroupBy(o => o.Key).Where(o => o.Count() > 1).Any(), "Duplicate root index entry.");

                    if (flushPageCatalog)
                    {
                        core.IO.PutPBuf(transaction, physicalIindex.DiskPath, findResult.Catalog);
                    }
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
            public PhysicalIndexPageCatalog IndexPageCatalog { get; set; }
            public object SyncObject { get; private set; } = new object();

            public RebuildIndexThreadParam(Transaction transaction, PhysicalSchema physicalSchema,
                PhysicalIndexPageCatalog indexPageCatalog, PhysicalIndex physicalIindex)
            {
                Transaction = transaction;
                PhysicalSchema = physicalSchema;
                PhysicalIindex = physicalIindex;
                IndexPageCatalog = indexPageCatalog;
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
                Utility.EnsureNotNull(PhysicalDocument);

                lock (param.SyncObject)
                {
                    InsertDocumentIntoIndex(param.Transaction, param.PhysicalSchema, param.PhysicalIindex, PhysicalDocument, param.IndexPageCatalog, false);
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
                Utility.EnsureNotNull(physicalIindex.DiskPath);
                Utility.EnsureNotNull(physicalSchema.DiskPath);

                var documentCatalog = core.Documents.GetPageDocuments(transaction, physicalSchema, LockOperation.Read).ToList();
                Utility.EnsureNotNull(documentCatalog);

                //Clear out the existing index pages.
                core.IO.PutPBuf(transaction, physicalIindex.DiskPath, new PhysicalIndexPageCatalog());

                var indexPageCatalog = core.IO.GetPBuf<PhysicalIndexPageCatalog>(transaction, physicalIindex.DiskPath, LockOperation.Write);
                Utility.EnsureNotNull(indexPageCatalog);

                var ptThreadCreation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.ThreadCreation);
                var threadParam = new RebuildIndexThreadParam(transaction, physicalSchema, indexPageCatalog, physicalIindex);
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

                core.IO.PutPBuf(transaction, physicalIindex.DiskPath, indexPageCatalog);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to rebuild single index for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void DeleteDocumentFromIndexes(Transaction transaction, PhysicalSchema physicalSchema, Guid documentId)
        {
            try
            {
                var indexCatalog = GetIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    DeleteDocumentFromIndex(transaction, physicalSchema, physicalIindex, documentId);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Multi-index upsert failed for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private bool RemoveDocumentFromLeaves(ref PhysicalIndexLeaves leaves, Guid documentId)
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
        /// Removes a document from an index. Locks the index page catalog for write.
        /// </summary>
        private void DeleteDocumentFromIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIindex, Guid documentId)
        {
            try
            {
                Utility.EnsureNotNull(physicalIindex.DiskPath);

                var PhysicalIndexPageCatalog = core.IO.GetPBuf<PhysicalIndexPageCatalog>(transaction, physicalIindex.DiskPath, LockOperation.Write);

                Utility.EnsureNotNull(PhysicalIndexPageCatalog);

                if (RemoveDocumentFromLeaves(ref PhysicalIndexPageCatalog.Leaves, documentId))
                {
                    core.IO.PutPBuf(transaction, physicalIindex.DiskPath, PhysicalIndexPageCatalog);
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
