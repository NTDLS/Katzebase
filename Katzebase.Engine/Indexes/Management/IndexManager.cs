using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Documents;
using Katzebase.Engine.Indexes.Matching;
using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Threading;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Katzebase.Engine.Indexes.Matching.IndexConstants;
using static Katzebase.Engine.KbLib.EngineConstants;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Indexes.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to indexes.
    /// </summary>
    public class IndexManager
    {
        private readonly Core core;
        internal IndexQueryHandlers QueryHandlers { get; set; }
        public IndexAPIHandlers APIHandlers { get; set; }

        public IndexManager(Core core)
        {
            this.core = core;
            try
            {
                QueryHandlers = new IndexQueryHandlers(core);
                APIHandlers = new IndexAPIHandlers(core);
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate indec manager.", ex);
                throw;
            }
        }

        internal void CreateIndex(Transaction transaction, string schemaName, KbIndex index, out Guid newId)
        {
            try
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
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void RebuildIndex(Transaction transaction, string schemaName, string indexName)
        {
            try
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
            catch (Exception ex)
            {
                core.Log.Write($"Failed to rebuild index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        internal void DropIndex(Transaction transaction, string schemaName, string indexName)
        {
            try
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
            catch (Exception ex)
            {
                core.Log.Write($"Failed to drop index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }


        internal Dictionary<uint, DocumentPointer> MatchDocuments(Transaction transaction, PhysicalIndexPages physicalIndexPages,
            IndexSelection indexSelection, ConditionSubset conditionSubset, Dictionary<string, string> conditionValues)
        {
            try
            {
                List<PhysicalIndexLeaf> workingPhysicalIndexLeaves = new() { physicalIndexPages.Root };
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

                    IEnumerable<PhysicalIndexLeaf>? foundLeaves = null;

                    var conditionValue = conditionValues[attribute.Field.ToLower()];

                    var ptIndexSeek = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.IndexSearch);

                    if (conditionField.LogicalQualifier == LogicalQualifier.Equals)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => w.Key == conditionValue).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.NotEquals)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => w.Key != conditionValue).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.GreaterThan)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchGreaterAsDecimal(w.Key, conditionValue) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.GreaterThanOrEqual)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchGreaterOrEqualAsDecimal(w.Key, conditionValue) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.LessThan)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLesserAsDecimal(w.Key, conditionValue) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.LessThanOrEqual)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLesserOrEqualAsDecimal(w.Key, conditionValue) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.Like)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLike(w.Key, conditionValue) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.NotLike)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLike(w.Key, conditionValue) == false).Select(s => s.Value));
                    else throw new KbNotImplementedException($"Logical qualifier has not been implemented for indexing: {conditionField.LogicalQualifier}");

                    ptIndexSeek?.StopAndAccumulate();

                    Utility.EnsureNotNull(foundLeaves);

                    if (foundLeaves.FirstOrDefault()?.Documents?.Any() == true) //We found documents, we are at the base of the index.
                    {
                        return foundLeaves.SelectMany(o => o.Documents ?? new List<PhysicalIndexEntry>()).ToDictionary(o => o.DocumentId, o => new DocumentPointer(o.PageNumber, o.DocumentId));
                    }

                    //Drop down to the next leve in the virtual tree we are building.
                    workingPhysicalIndexLeaves = new List<PhysicalIndexLeaf>();
                    workingPhysicalIndexLeaves.AddRange(foundLeaves);

                    if (foundAnything == false)
                    {
                        foundAnything = foundLeaves.Any();
                    }
                }

                if (foundAnything == false)
                {
                    return new Dictionary<uint, DocumentPointer>();
                }

                Utility.EnsureNotNull(workingPhysicalIndexLeaves);

                //This is an index scan.
                var ptIndexDistillation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.IndexDistillation);
                //If we got here then we didnt get a full match and will need to add all of the child-leaf document IDs for later elimination.
                var resultingDocuments = DistillIndexLeaves(workingPhysicalIndexLeaves);
                ptIndexDistillation?.StopAndAccumulate();

                return resultingDocuments;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to match index documents for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Finds document IDs given a set of conditions.
        /// </summary>
        internal Dictionary<uint, DocumentPointer> MatchDocuments(Transaction transaction,
                    PhysicalIndexPages physicalIndexPages, IndexSelection indexSelection, ConditionSubset conditionSubset, string workingSchemaPrefix)
        {
            try
            {
                List<PhysicalIndexLeaf> workingPhysicalIndexLeaves = new() { physicalIndexPages.Root };
                //List<PhysicalIndexLeaf> lastFoundPhysicalIndexLeaves = new();

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

                    IEnumerable<PhysicalIndexLeaf>? foundLeaves = null;

                    var ptIndexSeek = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.IndexSearch);

                    if (conditionField.LogicalQualifier == LogicalQualifier.Equals)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => w.Key == conditionField.Right.Value).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.NotEquals)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => w.Key != conditionField.Right.Value).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.GreaterThan)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchGreaterAsDecimal(w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.GreaterThanOrEqual)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchGreaterOrEqualAsDecimal(w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.LessThan)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLesserAsDecimal(w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.LessThanOrEqual)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLesserOrEqualAsDecimal(w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.Like)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLike(w.Key, conditionField.Right.Value) == true).Select(s => s.Value));
                    else if (conditionField.LogicalQualifier == LogicalQualifier.NotLike)
                        foundLeaves = workingPhysicalIndexLeaves.SelectMany(o => o.Children.Where(w => Condition.IsMatchLike(w.Key, conditionField.Right.Value) == false).Select(s => s.Value));
                    else throw new KbNotImplementedException($"Logical qualifier has not been implemented for indexing: {conditionField.LogicalQualifier}");

                    ptIndexSeek?.StopAndAccumulate();

                    Utility.EnsureNotNull(foundLeaves);

                    if (foundLeaves.FirstOrDefault()?.Documents?.Any() == true) //We found documents, we are at the base of the index.
                    {
                        return foundLeaves.SelectMany(o => o.Documents ?? new List<PhysicalIndexEntry>()).ToDictionary(o => o.DocumentId, o => new DocumentPointer(o.PageNumber, o.DocumentId));
                    }

                    //Drop down to the next leve in the virtual tree we are building.
                    workingPhysicalIndexLeaves = new List<PhysicalIndexLeaf>();
                    workingPhysicalIndexLeaves.AddRange(foundLeaves);

                    if (foundAnything == false)
                    {
                        foundAnything = foundLeaves.Any();
                    }
                }

                if (foundAnything == false)
                {
                    return new Dictionary<uint, DocumentPointer>();
                }

                Utility.EnsureNotNull(workingPhysicalIndexLeaves);

                //This is an index scan.
                var ptIndexDistillation = transaction.PT?.CreateDurationTracker(PerformanceTraceCumulativeMetricType.IndexDistillation);
                //If we got here then we didnt get a full match and will need to add all of the child-leaf document IDs for later elimination.
                var resultingDocuments = DistillIndexLeaves(workingPhysicalIndexLeaves);
                ptIndexDistillation?.StopAndAccumulate();

                return resultingDocuments;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to match index documents for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private Dictionary<uint, DocumentPointer> DistillIndexLeaves(List<PhysicalIndexLeaf> physicalIndexLeaves)
        {
            var result = new List<DocumentPointer>();

            foreach (var leaf in physicalIndexLeaves)
            {
                result.AddRange(DistillIndexLeaves(leaf));
            }

            return result.ToDictionary(o => o.DocumentId, o => o);
        }

        /// <summary>
        /// Traverse to the bottom of the index tree (from whatever starting point is passed in) and return a list of all documentids.
        /// </summary>
        /// <param name="indexEntires"></param>
        /// <returns></returns>
        private List<DocumentPointer> DistillIndexLeaves(PhysicalIndexLeaf physicalIndexLeaf)
        {
            try
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

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to distill index leaves.", ex);
                throw;
            }
        }

        internal PhysicalIndexCatalog AcquireIndexCatalog(Transaction transaction, string schemaName, LockOperation intendedOperation)
        {
            try
            {
                var physicalSchema = core.Schemas.Acquire(transaction, schemaName, intendedOperation);
                return AcquireIndexCatalog(transaction, physicalSchema, intendedOperation);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to acquire index catalog for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        public string MakeIndexFileName(string indexName) => $"@Index_{0}_Pages_{Helpers.MakeSafeFileName(indexName)}.PBuf";


        internal PhysicalIndexCatalog AcquireIndexCatalog(Transaction transaction, PhysicalSchema physicalSchema, LockOperation intendedOperation)
        {
            try
            {
                var indexCatalog = core.IO.GetJson<PhysicalIndexCatalog>(transaction, physicalSchema.IndexCatalogFilePath(), intendedOperation);

                indexCatalog.DiskPath = physicalSchema.IndexCatalogFilePath();

                foreach (var index in indexCatalog.Collection)
                {
                    index.DiskPath = Path.Combine(physicalSchema.DiskPath, MakeIndexFileName(index.Name));
                }

                return indexCatalog;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to acquire index catalog for process id {transaction.ProcessId}.", ex);
                throw;
            }
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
                core.Log.Write($"Failed to get index search tokens for process {transaction.ProcessId}.", ex);
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
                    else
                    {
                        break;
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
                core.Log.Write($"Failed to locate index extent for process {transaction.ProcessId}.", ex);
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
                core.Log.Write($"Failed to update document into indexes for process id {transaction.ProcessId}.", ex);
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
                core.Log.Write($"Failed to insert document into indexes for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts an index entry for a single document into a single index using the file name from the index object.
        /// </summary>
        private void InsertDocumentIntoIndex(Transaction transaction, PhysicalSchema physicalSchema,
            PhysicalIndex physicalIindex, PhysicalDocument document, DocumentPointer documentPointer)
        {
            try
            {
                var physicalIndexPages = core.IO.GetPBuf<PhysicalIndexPages>(transaction, physicalIindex.DiskPath, LockOperation.Write);
                InsertDocumentIntoIndex(transaction, physicalSchema, physicalIindex, document, documentPointer, physicalIndexPages, true);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to insert document into index for process id {transaction.ProcessId}.", ex);
                throw;
            }
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
                core.Log.Write($"Failed to insert document into index for process id {transaction.ProcessId}.", ex);
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
            try
            {
                Utility.EnsureNotNull(param);

                while (pool.ContinueToProcessQueue)
                {
                    param.Transaction.EnsureActive();

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
            catch (Exception ex)
            {
                core.Log.Write($"Failed to rebuild index by thread.", ex);
                throw;
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
                    if (threadPool.HasException)
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
                core.Log.Write($"Failed to rebuild index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Removes a collection of document from all of the indexes on the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documentPointer"></param>
        internal void RemoveDocumentsFromIndexes(Transaction transaction, PhysicalSchema physicalSchema, IEnumerable<DocumentPointer> documentPointers)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIindex in indexCatalog.Collection)
                {
                    RemoveDocumentsFromIndex(transaction, physicalIindex, documentPointers);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to delete document from indexes for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }


        private long RemoveDocumentsFromLeaves(PhysicalIndexLeaf leaf, IEnumerable<DocumentPointer> documentPointers)
        {
            return RemoveDocumentsFromLeaves(leaf, documentPointers, documentPointers.Count());
        }

        private long RemoveDocumentsFromLeaves(PhysicalIndexLeaf leaf, IEnumerable<DocumentPointer> documentPointers, long maxCount)
        {
            long totalDeletes = 0;

            try
            {
                if (leaf.Documents?.Any() == true)
                {
                    foreach (var documentPointer in documentPointers)
                    {
                        totalDeletes += (leaf?.Documents.RemoveAll(o => o.PageNumber == documentPointer.PageNumber && o.DocumentId == documentPointer.DocumentId) ?? 0);
                        if (totalDeletes == maxCount)
                        {
                            break;
                        }

                    }
                    return totalDeletes;
                }

                foreach (var child in leaf.Children)
                {
                    totalDeletes += RemoveDocumentsFromLeaves(child.Value, documentPointers); //Dig to the bottom of each branch using recursion.
                    if (totalDeletes == maxCount)
                    {
                        break;
                    }
                }

                return totalDeletes;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to remove documents from index leaves.", ex);
                throw;
            }
        }

        /// <summary>
        /// Removes a collection of documents from an index. Locks the index page catalog for write.
        /// </summary>
        private void RemoveDocumentsFromIndex(Transaction transaction, PhysicalIndex physicalIindex, IEnumerable<DocumentPointer> documentPointers)
        {
            try
            {
                var physicalIndexPages = core.IO.GetPBuf<PhysicalIndexPages>(transaction, physicalIindex.DiskPath, LockOperation.Write);

                RemoveDocumentsFromLeaves(physicalIndexPages.Root, documentPointers);
                core.IO.PutPBuf(transaction, physicalIindex.DiskPath, physicalIndexPages);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to remove documents from index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }
    }
}
