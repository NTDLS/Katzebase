using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Expressions;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.Parsers;
using NTDLS.Katzebase.Parsers.Conditions;
using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.PersistentTypes.Index;
using NTDLS.Katzebase.PersistentTypes.Schema;
using NTDLS.Katzebase.Shared;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Parsers.Constants;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to indexes.
    /// </summary>
    public class IndexManager
    {
        private readonly EngineCore _core;
        internal IndexQueryHandlers QueryHandlers { get; private set; }
        public IndexAPIHandlers APIHandlers { get; private set; }

        internal IndexManager(EngineCore core)
        {
            _core = core;
            try
            {
                QueryHandlers = new IndexQueryHandlers(core);
                APIHandlers = new IndexAPIHandlers(core);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate index manager.", ex);
                throw;
            }
        }

        #region Create / Analyze / Rebuild / Drop.

        internal void CreateIndex(Transaction transaction, string schemaName, KbIndex index, out Guid newId)
        {
            try
            {
                if (index.Attributes.Count == 0)
                {
                    throw new KbInvalidArgumentException($"Index [{index.Name}] on [{schemaName}] has no attributes.");
                }

                var physicalIndex = PhysicalIndex.FromClientPayload(index);

                physicalIndex.Id = Guid.NewGuid();
                physicalIndex.Created = DateTime.UtcNow;
                physicalIndex.Modified = DateTime.UtcNow;

                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var existingIndex = AcquireIndex(transaction, physicalSchema, physicalIndex.Name, LockOperation.Write);
                if (existingIndex != null)
                {
                    throw new KbObjectAlreadyExistsException($"Index already exists: [{index.Name}].");
                }

                var indexCfName = new RdbKey(physicalIndex.Id);

                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);

                _core.IO.PutJson(transaction, rdb, KbColumnFamilyName.Indexes, indexCfName, physicalIndex);
                rdb.CreateColumnFamily(indexCfName);

                // Record the new index in the transaction's write set for proper commit/rollback handling.
                transaction.RecordCfCreate(rdb, indexCfName);

                RebuildIndex(transaction, physicalSchema, physicalIndex);

                newId = physicalIndex.Id;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal string AnalyzeIndex(Transaction transaction, string schemaName, string indexName)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);
                var physicalIndex = AcquireIndex(transaction, physicalSchema, indexName, LockOperation.Read)
                    ?? throw new KbObjectNotFoundException($"Index not found: [{indexName}].");

                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                var indexCF = rdb.GetColumnFamily(new RdbKey(physicalIndex.Id));

                long distinctKeys = 0;
                long totalDocRefs = 0;
                long totalKeyBytes = 0;
                long totalValueBytes = 0;
                int minDocsPerKey = int.MaxValue;
                int maxDocsPerKey = 0;
                long singleDocKeys = 0;

                using var iter = rdb.NewIterator(indexCF);
                for (iter.SeekToFirst(); iter.Valid(); iter.Next())
                {
                    transaction.EnsureActive();

                    var keyBytes = iter.Key();
                    var valueBytes = iter.Value();
                    int docCount = valueBytes.Length / sizeof(uint);

                    distinctKeys++;
                    totalDocRefs += docCount;
                    totalKeyBytes += keyBytes.Length;
                    totalValueBytes += valueBytes.Length;

                    if (docCount < minDocsPerKey) minDocsPerKey = docCount;
                    if (docCount > maxDocsPerKey) maxDocsPerKey = docCount;
                    if (docCount == 1) singleDocKeys++;
                }

                if (distinctKeys == 0)
                    minDocsPerKey = 0;

                double avgDocsPerKey = distinctKeys > 0 ? (double)totalDocRefs / distinctKeys : 0.0;

                // Selectivity: fraction of keys that are unique relative to total references.
                // 100% = perfectly selective (every key maps to exactly one document).
                double selectivity = totalDocRefs > 0 ? (double)distinctKeys / totalDocRefs * 100.0 : 100.0;

                var sb = new StringBuilder();
                sb.AppendLine("Index Analysis {");
                sb.AppendLine($"    Schema            : {physicalSchema.Name}");
                sb.AppendLine($"    Name              : {physicalIndex.Name}");
                sb.AppendLine($"    Id                : {physicalIndex.Id}");
                sb.AppendLine($"    Unique            : {physicalIndex.IsUnique}");
                sb.AppendLine($"    Created           : {physicalIndex.Created:u}");
                sb.AppendLine($"    Modified          : {physicalIndex.Modified:u}");
                sb.AppendLine($"    Attributes ({physicalIndex.Attributes.Count}) {{");
                foreach (var attr in physicalIndex.Attributes)
                    sb.AppendLine($"        {attr.Field}");
                sb.AppendLine("    }");
                sb.AppendLine($"    Distinct Keys     : {distinctKeys:N0}");
                sb.AppendLine($"    Total Doc Refs    : {totalDocRefs:N0}");
                sb.AppendLine($"    Single-Doc Keys   : {singleDocKeys:N0}");
                sb.AppendLine($"    Min Docs/Key      : {minDocsPerKey:N0}");
                sb.AppendLine($"    Max Docs/Key      : {maxDocsPerKey:N0}" + (maxDocsPerKey == 1 ? " (unique)" : ""));
                sb.AppendLine($"    Avg Docs/Key      : {avgDocsPerKey:N2}");
                sb.AppendLine($"    Key Data          : {totalKeyBytes / 1024.0:N2}k");
                sb.AppendLine($"    Value Data        : {totalValueBytes / 1024.0:N2}k");
                sb.AppendLine($"    Selectivity       : {selectivity:N2}%");
                sb.AppendLine("}");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal void DropIndex(Transaction transaction, string schemaName, string indexName)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                var physicalIndex = AcquireIndex(transaction, physicalSchema, indexName, LockOperation.Write);
                if (physicalIndex != null)
                {
                    _core.IO.DeleteKey(transaction, rdb, KbColumnFamilyName.Indexes, new RdbKey(physicalIndex.Id));
                    rdb.DropColumnFamily(new RdbKey(physicalIndex.Id));
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        #endregion

        #region Match Schema Documents by Conditions.

        /// <summary>
        /// Used for indexing operations for a groups of conditions.
        /// </summary>
        /// <param name="keyValues">For JOIN operations, contains the values of the joining document.
        /// For WHERE clause, values are stored in the conditions so this is not needed.</param>
        /// <returns></returns>
        internal HashSet<uint> MatchSchemaDocumentsByConditionsClause(
                    PhysicalSchema physicalSchema, IndexingConditionOptimization optimization,
                    PreparedQuery query, string workingSchemaPrefix, KbInsensitiveDictionary<string?>? keyValues = null)
        {
            HashSet<uint>? accumulatedResults = null;

            var ptIndexSearch = optimization.Transaction.Instrumentation.CreateToken(PerformanceCounter.IndexSearch, $"Schema: {workingSchemaPrefix}");

            //We aggregate the values for the entries into the ConditionGroup.IndexLookup,
            //  which contains all of the values for all entries in the group.
            //  For this reason, we do not perform index lookups on individual condition entries.
            foreach (var group in optimization.Conditions.Collection.OfType<ConditionGroup>().Where(group => group.IndexLookup != null))
            {
                var groupResults = MatchSchemaDocumentsByConditionsClauseRecursive(physicalSchema, optimization, group, query, keyValues);

                if (group.LogicalConnector == LogicalConnector.Or)
                {
                    accumulatedResults ??= new(); //Really though, we should never start with an OR connector...

                    var ptDocumentPointerUnion = optimization.Transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerUnion);
                    accumulatedResults.UnionWith(groupResults);
                    ptDocumentPointerUnion?.StopAndAccumulate();
                }
                else // LogicalConnector.And || LogicalConnector.None
                {
                    var ptDocumentPointerIntersect = optimization.Transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
                    accumulatedResults = accumulatedResults.MaterializedIntersectWith(groupResults);
                    ptDocumentPointerIntersect?.StopAndAccumulate();
                }
            }

            ptIndexSearch?.StopAndAccumulate();

            return accumulatedResults ?? [];
        }

        private HashSet<uint> MatchSchemaDocumentsByConditionsClauseRecursive(
            PhysicalSchema physicalSchema, IndexingConditionOptimization optimization, ConditionGroup givenConditionGroup,
            PreparedQuery query, KbInsensitiveDictionary<string?>? keyValues = null)
        {
            var thisGroupResults = MatchSchemaDocumentsByIndexingConditionLookup(optimization.Transaction,
                query, givenConditionGroup.IndexLookup.EnsureNotNull(), physicalSchema, keyValues);

            foreach (var group in givenConditionGroup.Collection.OfType<ConditionGroup>().Where(o => o.IndexLookup != null))
            {
                var childGroupResults = MatchSchemaDocumentsByConditionsClauseRecursive(
                     physicalSchema, optimization, group, query, keyValues);

                if (group.LogicalConnector == LogicalConnector.Or)
                {
                    var ptDocumentPointerUnion = optimization.Transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerUnion);
                    thisGroupResults.UnionWith(childGroupResults);
                    ptDocumentPointerUnion?.StopAndAccumulate();
                }
                else // LogicalConnector.And || LogicalConnector.None
                {
                    var ptDocumentPointerIntersect = optimization.Transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
                    thisGroupResults = thisGroupResults.MaterializedIntersectWith(childGroupResults);
                    ptDocumentPointerIntersect?.StopAndAccumulate();
                }
            }

            return thisGroupResults;
        }

        private HashSet<uint> MatchSchemaDocumentsByIndexingConditionLookup(Transaction transaction, PreparedQuery query,
            IndexingConditionLookup indexLookup, PhysicalSchema physicalSchema, KbInsensitiveDictionary<string?>? keyValues)
        {
            try
            {
                var physicalIndex = indexLookup.IndexSelection.PhysicalIndex;
                var attributes = physicalIndex.Attributes;
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                var indexCF = rdb.GetColumnFamily(new RdbKey(physicalIndex.Id));

                HashSet<uint>? accumulatedResults = null;

                var firstAttrConditions = indexLookup.AttributeConditionSets[attributes[0].Field.EnsureNotNull()];

                foreach (var firstCondition in firstAttrConditions)
                {
                    var conditionResults = new HashSet<uint>();
                    var resolvedValue = ResolveConditionValue(transaction, query, firstCondition, keyValues);

                    // For equality on the first attribute, seek directly to the value's bytes.
                    // We use the raw value bytes (no trailing separator) so the seek lands ON
                    // the key rather than past it — stored keys have no trailing 0x00.
                    byte[]? seekBytes = firstCondition.Qualifier == LogicalQualifier.Equals && resolvedValue != null
                        ? IndexKeyBuilder.Build([resolvedValue])
                        : null;

                    using var iter = rdb.NewIterator(indexCF);
                    if (seekBytes != null)
                        iter.Seek(seekBytes);
                    else
                        iter.SeekToFirst();

                    for (; iter.Valid(); iter.Next())
                    {
                        var key = iter.Key();

                        // In equality-seek mode, stop once we've passed all keys for this value.
                        // A key belongs to this value if it equals seekBytes exactly (single-attribute)
                        // or starts with seekBytes followed by 0x00 (compound key).
                        if (seekBytes != null)
                        {
                            if (key.Length < seekBytes.Length) break;
                            if (!key.AsSpan(0, seekBytes.Length).SequenceEqual(seekBytes)) break;
                            if (key.Length > seekBytes.Length && key[seekBytes.Length] != 0x00) continue;
                        }

                        var fieldValues = IndexKeyBuilder.DecodeFieldValues(key);

                        // Apply the first attribute's condition.
                        if (!ConditionEntry.IsMatch(fieldValues[0], firstCondition.Qualifier, resolvedValue))
                            continue;

                        // Apply conditions for any remaining attributes in a compound index.
                        bool allMatch = true;
                        for (int depth = 1; depth < attributes.Count && allMatch; depth++)
                        {
                            if (!indexLookup.AttributeConditionSets.TryGetValue(
                                    attributes[depth].Field.EnsureNotNull(), out var depthConditions))
                                continue;

                            string? fieldValue = depth < fieldValues.Length ? fieldValues[depth] : null;
                            foreach (var depthCondition in depthConditions)
                            {
                                var depthValue = ResolveConditionValue(transaction, query, depthCondition, keyValues);
                                if (!ConditionEntry.IsMatch(fieldValue, depthCondition.Qualifier, depthValue))
                                {
                                    allMatch = false;
                                    break;
                                }
                            }
                        }

                        if (allMatch)
                            conditionResults.UnionWith(IndexKeyBuilder.UnpackDocumentIds(iter.Value()));
                    }

                    var ptIntersect = transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
                    accumulatedResults = accumulatedResults.MaterializedIntersectWith(conditionResults);
                    ptIntersect?.StopAndAccumulate();

                    if (accumulatedResults.Count == 0)
                        break;
                }

                return accumulatedResults ?? [];
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Resolves the right-hand value of a condition, checking join key values first,
        /// then collapsed literals, then falling back to full scalar field collapse.
        /// </summary>
        private static string? ResolveConditionValue(Transaction transaction, PreparedQuery query,
            ConditionEntry condition, KbInsensitiveDictionary<string?>? keyValues)
        {
            if (keyValues?.TryGetValue(condition.Right.Value.EnsureNotNull(), out string? keyValue) == true)
                return keyValue;

            if (condition.Right is QueryFieldCollapsedValue collapsedValue)
                return collapsedValue.Value;

            return condition.Right.CollapseScalarQueryField(transaction, query,
                query.Conditions.FieldCollection, keyValues ?? new())?.ToLowerInvariant();
        }

        #endregion

        #region Index Insert.

        /// <summary>
        /// Inserts an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void InsertDocumentIntoIndexes(Transaction transaction,
            PhysicalSchema physicalSchema, PhysicalDocument physicalDocument, uint documentId)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                //Loop though each index in the schema.
                foreach (var physicalIndex in indexCatalog)
                {
                    InsertDocumentIntoIndex(transaction, physicalSchema, physicalIndex, physicalDocument, documentId);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void InsertDocumentsIntoIndex(Transaction transaction,
            PhysicalSchema physicalSchema, PhysicalIndex physicalIndex, Dictionary<uint, PhysicalDocument> documents)
        {
            try
            {
                foreach (var document in documents)
                {
                    InsertDocumentIntoIndex(transaction, physicalSchema, physicalIndex, document.Value, document.Key);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }


        /// <summary>
        /// Inserts an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void InsertDocumentsIntoIndexes(Transaction transaction,
            PhysicalSchema physicalSchema, Dictionary<uint, PhysicalDocument> documents)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                foreach (var document in documents)
                {
                    //Loop though each index in the schema.
                    foreach (var physicalIndex in indexCatalog)
                    {
                        InsertDocumentIntoIndex(transaction, physicalSchema, physicalIndex, document.Value, document.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts an index entry for a single document into a single index using the file name from the index object.
        /// </summary>
        private void InsertDocumentIntoIndex(Transaction transaction,
            PhysicalSchema physicalSchema, PhysicalIndex physicalIndex, PhysicalDocument document, uint documentId)
        {
            try
            {
                var fieldValues = GetIndexSearchTokens(transaction, physicalIndex, document);
                if (fieldValues.Count != physicalIndex.Attributes.Count)
                    return; // one or more indexed fields are null/missing — document not indexed

                var key = IndexKeyBuilder.Build(fieldValues);
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                var indexCF = rdb.GetColumnFamily(new RdbKey(physicalIndex.Id));

                var existingBytes = rdb.Get(key, indexCF);
                var docIds = existingBytes != null
                    ? IndexKeyBuilder.UnpackDocumentIds(existingBytes)
                    : new List<uint>();

                if (physicalIndex.IsUnique && docIds.Count > 0)
                    throw new KbDuplicateKeyViolationException(
                        $"Duplicate key violation for index [{physicalIndex.Name}], values: [{string.Join("][", fieldValues)}]");

                docIds.Add(documentId);
                rdb.Put(key, IndexKeyBuilder.PackDocumentIds(docIds), indexCF);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        private static List<string> GetIndexSearchTokens(Transaction transaction, PhysicalIndex physicalIndex, PhysicalDocument document)
        {
            try
            {
                var result = new List<string>();

                foreach (var indexAttribute in physicalIndex.Attributes)
                {
                    if (document.Elements.TryGetValue(indexAttribute.Field.EnsureNotNull(), out string? documentValue))
                    {
                        if (documentValue != null) //TODO: How do we handle indexed NULL values?
                        {
                            result.Add(documentValue.ToLowerInvariant());
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        #endregion

        #region Index Update.

        /// <summary>
        /// Updates an index entry for a single document into each index in the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documents"></param>
        /// <param name="listOfModifiedFields">When not null, is used to limit the work needed to be done for index updates.</param>
        internal void UpdateDocumentsIntoIndexes(Transaction transaction, PhysicalSchema physicalSchema,
            Dictionary<uint, PhysicalDocument> documents, IEnumerable<string>? listOfModifiedFields)
        {
            if (documents.Any())
            {
                try
                {
                    var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                    foreach (var physicalIndex in indexCatalog)
                    {
                        if (listOfModifiedFields == null || physicalIndex.Attributes.Any(o => listOfModifiedFields.Contains(o.Field)))
                        {
                            RemoveDocumentsFromIndex(transaction, physicalSchema, physicalIndex, documents.Select(o => o.Key));
                            InsertDocumentsIntoIndex(transaction, physicalSchema, physicalIndex, documents);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to update document into indexes for process id {transaction.ProcessId}.", ex);
                    throw;
                }
            }
        }

        #endregion

        #region Index Delete.

        /// <summary>
        /// Removes a collection of document from all of the indexes on the schema.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="documentIds"></param>
        internal void RemoveDocumentsFromIndexes(Transaction transaction,
            PhysicalSchema physicalSchema, IEnumerable<uint> documentIds)
        {
            if (documentIds.Any())
            {
                try
                {
                    var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);

                    //Loop though each index in the schema.
                    foreach (var physicalIndex in indexCatalog)
                    {
                        RemoveDocumentsFromIndex(transaction, physicalSchema, physicalIndex, documentIds);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to delete document from indexes for process id {transaction.ProcessId}.", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Removes a collection of documents from an index. Locks the index page catalog for write.
        /// </summary>
        private void RemoveDocumentsFromIndex(Transaction transaction, PhysicalSchema physicalSchema,
            PhysicalIndex physicalIndex, IEnumerable<uint> documentIds)
        {
            if (documentIds.Any())
            {
                try
                {
                    var docIdSet = new HashSet<uint>(documentIds);
                    var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                    var indexCF = rdb.GetColumnFamily(new RdbKey(physicalIndex.Id));

                    // Re-read each document to get its field values so we can build the exact key to remove.
                    // Then do a targeted read-modify-write on only those keys.
                    foreach (var documentId in docIdSet)
                    {
                        transaction.EnsureActive();

                        var physicalDocument = _core.Documents.AcquireDocument(transaction, rdb, documentId, LockOperation.Read, false);
                        if (physicalDocument == null) continue;

                        var fieldValues = GetIndexSearchTokens(transaction, physicalIndex, physicalDocument);
                        if (fieldValues.Count != physicalIndex.Attributes.Count) continue;

                        var key = IndexKeyBuilder.Build(fieldValues);
                        var existingBytes = rdb.Get(key, indexCF);
                        if (existingBytes == null) continue;

                        var docIds = IndexKeyBuilder.UnpackDocumentIds(existingBytes);
                        docIds.RemoveAll(id => docIdSet.Contains(id));

                        if (docIds.Count == 0)
                            rdb.Remove(key, indexCF);
                        else
                            rdb.Put(key, IndexKeyBuilder.PackDocumentIds(docIds), indexCF);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to remove documents from index for process id {transaction.ProcessId}.", ex);
                    throw;
                }
            }
        }

        #endregion

        #region Rebuild Index.

        internal void RebuildIndex(Transaction transaction, string schemaName, string indexName)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var physicalIndex = AcquireIndex(transaction, schemaName, indexName, LockOperation.Write)
                    ?? throw new KbObjectNotFoundException($"Index not found: [{indexName}].");
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);

                RebuildIndex(transaction, physicalSchema, physicalIndex);

                physicalIndex.Modified = DateTime.UtcNow;

                _core.IO.PutJson(transaction, rdb, KbColumnFamilyName.Indexes, new RdbKey(physicalIndex.Id), physicalIndex);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to rebuild index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts all documents in a schema into a single index in the schema. Locks the index page catalog for write.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="physicalIndex"></param>
        private void RebuildIndex(Transaction transaction, PhysicalSchema physicalSchema, PhysicalIndex physicalIndex)
        {
            try
            {
                if (physicalIndex.Attributes.Count == 0)
                    throw new KbInvalidArgumentException($"Index [{physicalIndex.Name}] on [{physicalSchema.Name}] has no attributes.");

                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                var documentsCF = rdb.GetColumnFamily(KbColumnFamilyName.Documents);

                // Step 1: Drop and recreate the index column family to clear any existing entries.
                rdb.DropColumnFamily(new RdbKey(physicalIndex.Id));
                var indexCF = rdb.CreateColumnFamily(new RdbKey(physicalIndex.Id));

                // Step 2: Collect all document IDs with a sequential key-only scan (no deserialization).
                var documentIds = new List<uint>();
                using (var docIter = rdb.NewIterator(documentsCF))
                {
                    for (docIter.SeekToFirst(); docIter.Valid(); docIter.Next())
                        documentIds.Add(RdbKey.ConvertToUint(docIter.Key()));
                }

                // Step 3: Process documents in batches to keep memory bounded.
                // Each batch accumulates in parallel, writes to RocksDB, then is discarded.
                // Cross-batch key merging uses read-modify-write so non-unique index entries
                // from different batches that share the same key value are correctly combined.
                const int batchSize = 50_000;

                var ptWrite = transaction.Instrumentation.CreateToken(PerformanceCounter.IOWrite);

                for (int batchStart = 0; batchStart < documentIds.Count; batchStart += batchSize)
                {
                    var batchEnd = Math.Min(batchStart + batchSize, documentIds.Count);
                    var accumulator = new ConcurrentDictionary<string, (byte[] KeyBytes, ConcurrentBag<uint> DocIds)>(StringComparer.Ordinal);

                    var childPool = _core.ThreadPool.Indexing.CreateChildPool<uint>(_core.Settings.IndexingThreadPoolQueueDepth);
                    for (int i = batchStart; i < batchEnd; i++)
                    {
                        var documentId = documentIds[i];
                        childPool.Enqueue(documentId, (threadDocumentId) =>
                        {
                            transaction.EnsureActive();

                            // Read directly without transaction tracking: this rebuild already writes
                            // via raw WriteBatch (bypassing the transaction atom log), so tracking
                            // reads for rollback-cache-eviction would be both inconsistent and a
                            // source of unbounded memory growth across the full document set.
                            var physicalDocument = _core.IO.GetNotTracked<PhysicalDocument>(
                                rdb, KbColumnFamilyName.Documents, new RdbKey(threadDocumentId).Bytes, IOFormat.PBuf);
                            if (physicalDocument == null) return;

                            var fieldValues = GetIndexSearchTokens(transaction, physicalIndex, physicalDocument);
                            if (fieldValues.Count != physicalIndex.Attributes.Count)
                                return; // document is missing one or more indexed fields — skip

                            var keyBytes = IndexKeyBuilder.Build(fieldValues);
                            var keyHex = Convert.ToHexStringLower(keyBytes);

                            var entry = accumulator.GetOrAdd(keyHex, _ => (keyBytes, new ConcurrentBag<uint>()));
                            entry.DocIds.Add(threadDocumentId);
                        });
                    }
                    childPool.WaitForCompletion(); // Propagates worker exceptions as AggregateException.

                    // Check uniqueness within this batch and against any previously written batches.
                    if (physicalIndex.IsUnique)
                    {
                        foreach (var (_, (keyBytes, docIds)) in accumulator)
                        {
                            if (docIds.Count > 1 || rdb.Get(keyBytes, indexCF) != null)
                            {
                                throw new KbDuplicateKeyViolationException(
                                    $"Duplicate key violation rebuilding unique index [{physicalIndex.Name}] on [{physicalSchema.Name}].");
                            }
                        }
                    }

                    using var batch = new RocksDbSharp.WriteBatch();
                    foreach (var (_, (keyBytes, docIds)) in accumulator)
                    {
                        var existingBytes = rdb.Get(keyBytes, indexCF);
                        var allDocIds = existingBytes != null
                            ? IndexKeyBuilder.UnpackDocumentIds(existingBytes)
                            : new List<uint>();
                        allDocIds.AddRange(docIds);
                        batch.Put(keyBytes, IndexKeyBuilder.PackDocumentIds(allDocIds), indexCF.Handle);
                    }
                    rdb.Write(batch);
                }
                ptWrite?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to rebuild index for process id {transaction.ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        internal List<PhysicalIndex> AcquireIndexCatalog(Transaction transaction, string schemaName, LockOperation lockOp)
        {
            try
            {
                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, lockOp);
                return AcquireIndexCatalog(transaction, physicalSchema, lockOp);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal List<PhysicalIndex> AcquireIndexCatalog(Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockOp)
        {
            try
            {
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                var indexes = _core.IO.GetJsonList<PhysicalIndex>(transaction, rdb, KbColumnFamilyName.Indexes, lockOp);
                return indexes;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal PhysicalIndex? AcquireIndex(Transaction transaction, string schemaName, string indexName, LockOperation lockOp)
        {
            var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, lockOp);
            var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, lockOp);

            return indexCatalog.FirstOrDefault(o => o.Name.Equals(indexName, StringComparison.InvariantCultureIgnoreCase));
        }

        internal PhysicalIndex? AcquireIndex(Transaction transaction, PhysicalSchema physicalSchema, string indexName, LockOperation lockOp)
        {
            var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, lockOp);
            return indexCatalog.FirstOrDefault(o => o.Name.Equals(indexName, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
