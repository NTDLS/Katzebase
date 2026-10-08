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
using NTDLS.Katzebase.PersistentTypes.Atomicity;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.PersistentTypes.Index;
using NTDLS.Katzebase.PersistentTypes.Schema;
using NTDLS.Katzebase.Shared;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// Deserialized index catalogs, cached per documents RDB instance. The catalog is read for every inserted, updated
        /// and deleted document and for every query, but only changes on CREATE/DROP/REBUILD INDEX. Keying by the Rdb
        /// instance (rather than path) means a dropped and re-created schema can never observe a stale catalog.
        /// </summary>
        private readonly ConditionalWeakTable<Rdb, List<PhysicalIndex>> _catalogCache = new();
        private readonly Lock _catalogCacheLock = new();
        /// <summary>
        /// Incremented on every invalidation so that a catalog loaded concurrently with an invalidation is not cached.
        /// </summary>
        private long _catalogGeneration;

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
                physicalIndex.StorageVersion = PhysicalIndex.CurrentStorageVersion;

                var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
                var existingIndex = AcquireIndex(transaction, physicalSchema, physicalIndex.Name, LockOperation.Write);
                if (existingIndex != null)
                {
                    throw new KbObjectAlreadyExistsException($"Index already exists: [{index.Name}].");
                }

                var indexCfName = new RdbKey(physicalIndex.Id);

                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);

                InvalidateIndexCatalog(transaction, rdb);
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

                EnsureCurrentStorageVersion(physicalIndex);

                //There is one entry per document and entries with the same values are adjacent,
                //  so a "key" (distinct value) is a run of consecutive entries with the same value part.
                byte[]? currentValuePart = null;
                int docCount = 0;

                void CompleteKey()
                {
                    if (docCount == 0) return;
                    distinctKeys++;
                    if (docCount < minDocsPerKey) minDocsPerKey = docCount;
                    if (docCount > maxDocsPerKey) maxDocsPerKey = docCount;
                    if (docCount == 1) singleDocKeys++;
                }

                using var iter = rdb.NewIterator(indexCF);
                for (iter.SeekToFirst(); iter.Valid(); iter.Next())
                {
                    transaction.EnsureActive();

                    var keyBytes = iter.Key();
                    var valuePart = IndexKeyBuilder.GetValuePart(physicalIndex.IsUnique, keyBytes);

                    if (currentValuePart == null || !valuePart.SequenceEqual(currentValuePart))
                    {
                        CompleteKey();
                        currentValuePart = valuePart.ToArray();
                        docCount = 0;
                    }

                    docCount++;
                    totalDocRefs++;
                    totalKeyBytes += keyBytes.Length;
                    totalValueBytes += iter.Value().Length;
                }
                CompleteKey();

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
                    InvalidateIndexCatalog(transaction, rdb);
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
            //All conditions in a group are ANDed, so each of the group's index lookups yields a superset of the matching
            //  documents and the group's candidates are their intersection. Lookups are ordered most selective first.
            HashSet<uint>? thisGroupResults = null;

            foreach (var indexLookup in givenConditionGroup.IndexLookups)
            {
                if (thisGroupResults != null
                    && (thisGroupResults.Count <= 1 || IndexingConditionOptimization.EstimateLookupCost(indexLookup) > IndexingConditionOptimization.PointLookupCost))
                {
                    //We already have a small candidate set: the WHERE clause is re-evaluated against every candidate
                    //  document anyway, so scanning another index to narrow it further is not worth the cost.
                    break;
                }

                var lookupResults = MatchSchemaDocumentsByIndexingConditionLookup(optimization.Transaction,
                    query, indexLookup, physicalSchema, keyValues);

                var ptDocumentPointerIntersect = optimization.Transaction.Instrumentation.CreateToken(PerformanceCounter.DocumentPointerIntersect);
                thisGroupResults = thisGroupResults.MaterializedIntersectWith(lookupResults);
                ptDocumentPointerIntersect?.StopAndAccumulate();
            }

            thisGroupResults ??= new();

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
                EnsureCurrentStorageVersion(physicalIndex);
                var attributes = physicalIndex.Attributes;
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                var indexCF = rdb.GetColumnFamily(new RdbKey(physicalIndex.Id));

                //Resolve the right-hand value of every condition once up front, rather than once per scanned index key.
                var attributeConditions = new List<(ConditionEntry Condition, string? Value, string? HighValue)>?[attributes.Count];
                for (int depth = 0; depth < attributes.Count; depth++)
                {
                    if (indexLookup.AttributeConditionSets.TryGetValue(attributes[depth].Field.EnsureNotNull(), out var conditions))
                    {
                        attributeConditions[depth] = conditions
                            .Select(condition => (condition, ResolveConditionValue(transaction, query, condition.Right, keyValues),
                                condition.RightHigh == null ? null : ResolveConditionValue(transaction, query, condition.RightHigh, keyValues))).ToList();
                    }
                }

                //Index keys begin [field1][0x00][field2][0x00]..., so the leading attributes that are constrained by an equality
                //  form a key prefix that we can seek directly to, rather than scanning the index from the beginning.
                var equalityPrefix = new List<string>();
                for (int depth = 0; depth < attributes.Count; depth++)
                {
                    var equalityValue = attributeConditions[depth]?
                        .Where(o => o.Condition.Qualifier == LogicalQualifier.Equals && o.Value != null)
                        .Select(o => o.Value).FirstOrDefault();
                    if (equalityValue == null)
                    {
                        break;
                    }
                    equalityPrefix.Add(equalityValue);
                }

                var results = new HashSet<uint>();
                bool isUnique = physicalIndex.IsUnique;

                //Entries within the equality prefix already satisfy the equalities that formed it. Only when there are other
                //  conditions (e.g. a range on a trailing attribute) do the entry's field values need to be checked.
                bool requiresFiltering = attributeConditions.Sum(o => o?.Count ?? 0) > equalityPrefix.Count;

                if (isUnique && equalityPrefix.Count == attributes.Count)
                {
                    //Unique index with every attribute pinned by an equality: the values are the entire key, a single Get.
                    var uniqueKey = IndexKeyBuilder.BuildPrefix(equalityPrefix);
                    var documentIdBytes = rdb.Get(uniqueKey, indexCF);
                    if (documentIdBytes != null && (!requiresFiltering || IsIndexKeyMatch([.. equalityPrefix])))
                    {
                        results.Add(IndexKeyBuilder.DecodeDocumentId(isUnique, uniqueKey, documentIdBytes));
                    }
                    return results;
                }

                //Entries begin with their values, so all documents matching the equality prefix are adjacent and are
                //  found with a single seek followed by a scan (when every attribute is pinned, the scan only visits matches).
                var seekPrefix = equalityPrefix.Count > 0 ? IndexKeyBuilder.BuildPrefix(equalityPrefix) : null;

                using var iter = rdb.NewIterator(indexCF);
                if (seekPrefix != null)
                    iter.Seek(seekPrefix);
                else
                    iter.SeekToFirst();

                //Consecutive entries usually share their values, so remember the outcome for the last distinct value.
                byte[]? lastValuePart = null;
                bool lastValueMatched = false;

                for (; iter.Valid(); iter.Next())
                {
                    var key = iter.Key();

                    if (seekPrefix != null && !key.AsSpan().StartsWith(seekPrefix))
                    {
                        break; //We have passed all entries that share the equality prefix.
                    }

                    if (requiresFiltering)
                    {
                        var valuePart = IndexKeyBuilder.GetValuePart(isUnique, key);
                        if (lastValuePart == null || !valuePart.SequenceEqual(lastValuePart))
                        {
                            lastValuePart = valuePart.ToArray();
                            lastValueMatched = IsIndexKeyMatch(IndexKeyBuilder.DecodeFieldValues(isUnique, key));
                        }

                        if (!lastValueMatched)
                        {
                            continue;
                        }
                    }

                    results.Add(IndexKeyBuilder.DecodeDocumentId(isUnique, key, isUnique ? iter.Value() : []));
                }

                return results;

                //All conditions on all attributes must match (conditions within a group are ANDed).
                bool IsIndexKeyMatch(string[] fieldValues)
                {
                    for (int depth = 0; depth < attributes.Count; depth++)
                    {
                        var conditions = attributeConditions[depth];
                        if (conditions == null)
                        {
                            continue;
                        }

                        string? fieldValue = depth < fieldValues.Length ? fieldValues[depth] : null;
                        foreach (var (condition, value, highValue) in conditions)
                        {
                            if (!ConditionEntry.IsMatch(fieldValue, condition.Qualifier, value, highValue))
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Resolves a right-hand value of a condition (the value, or the high value of a BETWEEN), checking join key
        /// values first, then collapsed literals, then falling back to full scalar field collapse.
        /// </summary>
        private static string? ResolveConditionValue(Transaction transaction, PreparedQuery query,
            IQueryField field, KbInsensitiveDictionary<string?>? keyValues)
        {
            if (keyValues?.TryGetValue(field.Value.EnsureNotNull(), out string? keyValue) == true)
                return keyValue;

            if (field is QueryFieldCollapsedValue collapsedValue)
                return collapsedValue.Value;

            return field.CollapseScalarQueryField(transaction, query,
                query.Conditions.FieldCollection, keyValues ?? new())?.ToLowerInvariant();
        }

        #endregion

        #region Index Insert.

        /// <summary>
        /// Inserts index entries for a newly created document into each index in the schema.
        /// </summary>
        internal void InsertDocumentIntoIndexes(Transaction transaction,
            PhysicalSchema physicalSchema, PhysicalDocument physicalDocument, uint documentId)
        {
            try
            {
                var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);
                if (indexCatalog.Count == 0)
                {
                    return;
                }

                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);

                //Loop though each index in the schema.
                foreach (var physicalIndex in indexCatalog)
                {
                    InsertDocumentIntoIndex(transaction, rdb, physicalIndex, physicalDocument.Elements, documentId, isNewDocument: true);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <param name="isNewDocument">True when the document id was just allocated, so it cannot already have index entries
        /// and the existence check before adding its entry can be skipped.</param>
        private static void InsertDocumentIntoIndex(Transaction transaction,
            Rdb rdb, PhysicalIndex physicalIndex, KbInsensitiveDictionary<string?> elements, uint documentId, bool isNewDocument = false)
        {
            try
            {
                EnsureCurrentStorageVersion(physicalIndex);

                var fieldValues = GetIndexSearchTokens(transaction, physicalIndex, elements);
                if (fieldValues.Count != physicalIndex.Attributes.Count)
                    return; // one or more indexed fields are null/missing — document not indexed

                var indexCF = rdb.GetColumnFamily(new RdbKey(physicalIndex.Id));

                var entryKey = IndexKeyBuilder.BuildEntryKey(physicalIndex.IsUnique, fieldValues, documentId);
                var entryValue = IndexKeyBuilder.BuildEntryValue(physicalIndex.IsUnique, documentId);

                if (physicalIndex.IsUnique)
                {
                    //The values are the whole key of a unique index, so an existing entry for another document is a violation.
                    var existingValue = rdb.Get(entryKey, indexCF);
                    if (existingValue != null)
                    {
                        if (IndexKeyBuilder.DecodeDocumentId(true, entryKey, existingValue) == documentId)
                        {
                            return; //Already indexed.
                        }

                        throw new KbDuplicateKeyViolationException(
                            $"Duplicate key violation for index [{physicalIndex.Name}], values: [{string.Join("][", fieldValues)}]");
                    }

                    AddIndexEntry(transaction, rdb, indexCF, entryKey, entryValue, isKnownAbsent: true);
                }
                else
                {
                    AddIndexEntry(transaction, rdb, indexCF, entryKey, entryValue, isKnownAbsent: isNewDocument);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Removes the entries of the given documents that are indexed with the values [fieldValues].
        /// </summary>
        private static void RemoveDocumentsFromIndexKey(Transaction transaction, Rdb rdb, RdbColumnFamily indexCF,
            PhysicalIndex physicalIndex, List<string> fieldValues, IEnumerable<uint> documentIds)
        {
            foreach (var documentId in documentIds)
            {
                RemoveIndexEntry(transaction, rdb, indexCF,
                    IndexKeyBuilder.BuildEntryKey(physicalIndex.IsUnique, fieldValues, documentId),
                    IndexKeyBuilder.BuildEntryValue(physicalIndex.IsUnique, documentId));
            }
        }

        /// <summary>
        /// Adds an index entry, recording it in the transaction log so that it is removed if the transaction rolls back.
        /// Without this, a rolled back insert leaves index entries that point at documents which no longer exist
        /// (and permanently occupies unique key values).
        /// </summary>
        /// <param name="isKnownAbsent">The entry is known not to exist (the document is new), skip the existence check.</param>
        private static void AddIndexEntry(Transaction transaction, Rdb rdb, RdbColumnFamily indexCF,
            byte[] entryKey, byte[] entryValue, bool isKnownAbsent = false)
        {
            if (!isKnownAbsent && rdb.Get(entryKey, indexCF) != null)
            {
                return; //Already indexed.
            }

            var rdbKey = new RdbKey(entryKey);
            transaction.RecordKeyCreate(rdb, indexCF.Name, rdbKey, MakeIndexEntryCacheKey(rdb, indexCF, rdbKey));
            rdb.Put(entryKey, entryValue, indexCF);
        }

        /// <summary>
        /// Removes an index entry, recording it in the transaction log so that it is restored if the transaction rolls back.
        /// </summary>
        /// <param name="expectedValue">The entry is only removed if it has this value. For unique indexes the value is the
        /// document id, which guarantees that one document never removes another document's entry.</param>
        private static void RemoveIndexEntry(Transaction transaction, Rdb rdb, RdbColumnFamily indexCF, byte[] entryKey, byte[] expectedValue)
        {
            var existingValue = rdb.Get(entryKey, indexCF);
            if (existingValue == null || !existingValue.AsSpan().SequenceEqual(expectedValue))
            {
                return; //Not indexed (for this document).
            }

            var rdbKey = new RdbKey(entryKey);
            transaction.RecordKeyDelete(rdb, indexCF.Name, rdbKey, MakeIndexEntryCacheKey(rdb, indexCF, rdbKey), existingValue);
            rdb.Remove(entryKey, indexCF);
        }

        private static CacheKey MakeIndexEntryCacheKey(Rdb rdb, RdbColumnFamily indexCF, RdbKey rdbKey)
            => new(rdb.Path, $"{rdb.Path}:{indexCF.Name}:{rdbKey}");

        /// <summary>
        /// Indexes stored in the previous layout (one entry per value holding a list of document ids) cannot be maintained
        /// or read by this version of the engine and must be rebuilt. Fail loudly rather than corrupting or misreading them.
        /// </summary>
        private static void EnsureCurrentStorageVersion(PhysicalIndex physicalIndex)
        {
            if (!physicalIndex.IsCurrentStorageVersion())
            {
                throw new KbEngineException($"Index [{physicalIndex.Name}] was created by an older version of Katzebase and must be rebuilt"
                    + $" before it can be used: REBUILD INDEX {physicalIndex.Name} ON <schema>");
            }
        }


        private static List<string> GetIndexSearchTokens(Transaction transaction, PhysicalIndex physicalIndex, PhysicalDocument document)
            => GetIndexSearchTokens(transaction, physicalIndex, document.Elements);

        private static List<string> GetIndexSearchTokens(Transaction transaction, PhysicalIndex physicalIndex, KbInsensitiveDictionary<string?> elements)
        {
            try
            {
                var result = new List<string>();

                foreach (var indexAttribute in physicalIndex.Attributes)
                {
                    if (elements.TryGetValue(indexAttribute.Field.EnsureNotNull(), out string? documentValue))
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
        /// Moves updated documents to their new entries in each affected index of the schema.
        /// </summary>
        /// <param name="documents">The updated documents along with a copy of their elements from before the update.
        /// The original values are required because the index key to remove the document from is derived from them.</param>
        /// <param name="listOfModifiedFields">When not null, is used to limit the work needed to be done for index updates.</param>
        internal void UpdateDocumentsIntoIndexes(Transaction transaction, PhysicalSchema physicalSchema,
            Dictionary<uint, (KbInsensitiveDictionary<string?> OriginalElements, PhysicalDocument Document)> documents,
            IEnumerable<string>? listOfModifiedFields)
        {
            if (documents.Count != 0)
            {
                try
                {
                    var indexCatalog = AcquireIndexCatalog(transaction, physicalSchema, LockOperation.Read);
                    var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);

                    foreach (var physicalIndex in indexCatalog)
                    {
                        if (listOfModifiedFields != null && !physicalIndex.Attributes.Any(o => listOfModifiedFields.Contains(o.Field)))
                        {
                            continue;
                        }

                        EnsureCurrentStorageVersion(physicalIndex);
                        var indexCF = rdb.GetColumnFamily(new RdbKey(physicalIndex.Id));

                        //Remove every document from its old entry before adding any to their new entries so that
                        //  unique indexes allow values to be swapped between documents within the same statement.
                        var moved = new List<(uint DocumentId, PhysicalDocument Document)>();
                        foreach (var document in documents)
                        {
                            transaction.EnsureActive();

                            var originalValues = GetIndexSearchTokens(transaction, physicalIndex, document.Value.OriginalElements);
                            var updatedValues = GetIndexSearchTokens(transaction, physicalIndex, document.Value.Document.Elements);

                            if (originalValues.SequenceEqual(updatedValues))
                            {
                                continue; //The indexed values did not change, so neither does the index entry.
                            }

                            if (originalValues.Count == physicalIndex.Attributes.Count)
                            {
                                RemoveDocumentsFromIndexKey(transaction, rdb, indexCF, physicalIndex, originalValues, [document.Key]);
                            }
                            moved.Add((document.Key, document.Value.Document));
                        }

                        foreach (var (documentId, document) in moved)
                        {
                            InsertDocumentIntoIndex(transaction, rdb, physicalIndex, document.Elements, documentId);
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
        /// Removes a collection of (not yet modified) documents from an index.
        /// </summary>
        private void RemoveDocumentsFromIndex(Transaction transaction, PhysicalSchema physicalSchema,
            PhysicalIndex physicalIndex, IEnumerable<uint> documentIds)
        {
            if (documentIds.Any())
            {
                try
                {
                    EnsureCurrentStorageVersion(physicalIndex);

                    var docIdSet = new HashSet<uint>(documentIds);
                    var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                    var indexCF = rdb.GetColumnFamily(new RdbKey(physicalIndex.Id));

                    // Re-read each document to get its field values so we can build the exact key to remove.
                    // Then do a targeted read-modify-write on only those keys.
                    foreach (var documentId in docIdSet)
                    {
                        transaction.EnsureActive();

                        var physicalDocument = _core.Documents.AcquireDocumentVirtual(transaction, rdb, documentId, LockOperation.Read, false);
                        if (physicalDocument == null) continue;

                        var fieldValues = GetIndexSearchTokens(transaction, physicalIndex, physicalDocument);
                        if (fieldValues.Count != physicalIndex.Attributes.Count) continue;

                        RemoveDocumentsFromIndexKey(transaction, rdb, indexCF, physicalIndex, fieldValues, [documentId]);
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

                InvalidateIndexCatalog(transaction, rdb);
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

                // Step 2: Read the documents with a single sequential scan, in batches to keep memory bounded. Each batch
                // is deserialized and keyed in parallel and written in one WriteBatch. Entries are one per document, so
                // batches never need to read or merge with each other's entries.
                //
                // The values come straight from the iterator, so each data block is read once. (Collecting the ids
                // first and then reading each document by id read every block once per document it contains, since
                // the block cache is disabled.) Values are read without transaction tracking: the rebuild writes via a
                // raw WriteBatch (bypassing the transaction atom log), so tracking reads for rollback-cache-eviction
                // would be both inconsistent and a source of unbounded memory growth across the full document set.
                const int batchSize = 50_000;
                const int chunkSize = 1_000;

                var ptWrite = transaction.Instrumentation.CreateToken(PerformanceCounter.IOWrite);

                using var docIter = rdb.NewIterator(documentsCF);
                docIter.SeekToFirst();

                var documents = new List<(uint DocumentId, byte[] Value)>(batchSize);

                while (true)
                {
                    transaction.EnsureActive();

                    documents.Clear();
                    for (; docIter.Valid() && documents.Count < batchSize; docIter.Next())
                    {
                        documents.Add((RdbKey.ConvertToUint(docIter.Key()), docIter.Value()));
                    }

                    if (documents.Count == 0)
                    {
                        break;
                    }

                    var entryChunks = new ConcurrentBag<List<(byte[] Key, byte[] Value)>>();

                    //Documents are handed to the pool in chunks: queueing one work item per document cost more than keying it.
                    var childPool = _core.ThreadPool.Indexing.CreateChildPool<(int Start, int End)>(_core.Settings.IndexingThreadPoolQueueDepth);
                    for (int chunkStart = 0; chunkStart < documents.Count; chunkStart += chunkSize)
                    {
                        childPool.Enqueue((chunkStart, Math.Min(chunkStart + chunkSize, documents.Count)), (range) =>
                        {
                            var chunkEntries = new List<(byte[] Key, byte[] Value)>(range.End - range.Start);

                            for (int i = range.Start; i < range.End; i++)
                            {
                                transaction.EnsureActive();

                                var (documentId, value) = documents[i];

                                PhysicalDocument? physicalDocument;
                                using (var input = new MemoryStream(value))
                                {
                                    physicalDocument = ProtoBuf.Serializer.Deserialize<PhysicalDocument>(input);
                                }
                                if (physicalDocument == null) continue;

                                var fieldValues = GetIndexSearchTokens(transaction, physicalIndex, physicalDocument);
                                if (fieldValues.Count != physicalIndex.Attributes.Count)
                                    continue; // document is missing one or more indexed fields — skip

                                chunkEntries.Add((IndexKeyBuilder.BuildEntryKey(physicalIndex.IsUnique, fieldValues, documentId),
                                    IndexKeyBuilder.BuildEntryValue(physicalIndex.IsUnique, documentId)));
                            }

                            entryChunks.Add(chunkEntries);
                        });
                    }
                    childPool.WaitForCompletion(); // Propagates worker exceptions as AggregateException.

                    var entries = entryChunks.SelectMany(o => o);

                    //The values are the whole key of a unique index, so a duplicate shows up as a key that was already
                    //  added in this batch or that was written by a previous batch.
                    if (physicalIndex.IsUnique)
                    {
                        var batchKeys = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var (key, _) in entries)
                        {
                            if (!batchKeys.Add(Convert.ToHexString(key)) || rdb.Get(key, indexCF) != null)
                            {
                                throw new KbDuplicateKeyViolationException(
                                    $"Duplicate key violation rebuilding unique index [{physicalIndex.Name}] on [{physicalSchema.Name}],"
                                    + $" values: [{string.Join("][", IndexKeyBuilder.DecodeFieldValues(true, key))}].");
                            }
                        }
                    }

                    using var batch = new RocksDbSharp.WriteBatch();
                    foreach (var (key, value) in entries)
                    {
                        batch.Put(key, value, indexCF.Handle);
                    }
                    rdb.Write(batch);
                }

                physicalIndex.StorageVersion = PhysicalIndex.CurrentStorageVersion;

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

                //Only shared reads are served from the cache. Writers (and any transaction that has modified this catalog
                //  but not yet committed) read it directly so they see their own changes and never publish them early.
                if ((lockOp != LockOperation.Read && lockOp != LockOperation.Stability)
                    || transaction.ModifiedIndexCatalogs.ContainsKey(rdb.Path))
                {
                    return _core.IO.GetJsonList<PhysicalIndex>(transaction, rdb, KbColumnFamilyName.Indexes, lockOp);
                }

                //Take the same lock that reading the catalog from disk would, so that locking semantics are unchanged.
                transaction.LockSingleObject(lockOp, CacheManager.MakeCacheKey(rdb.Path, KbColumnFamilyName.Indexes));

                long generation;
                lock (_catalogCacheLock)
                {
                    if (_catalogCache.TryGetValue(rdb, out var cachedIndexes))
                    {
                        return new List<PhysicalIndex>(cachedIndexes);
                    }
                    generation = _catalogGeneration;
                }

                var indexes = _core.IO.GetJsonList<PhysicalIndex>(transaction, rdb, KbColumnFamilyName.Indexes, lockOp);

                lock (_catalogCacheLock)
                {
                    //Don't cache a catalog that may have been read while it was being changed.
                    if (generation == _catalogGeneration)
                    {
                        _catalogCache.AddOrUpdate(rdb, new List<PhysicalIndex>(indexes));
                    }
                }

                return indexes;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Must be called before a transaction modifies the index catalog of a schema. Evicts the cached catalog immediately
        /// and again when the transaction commits or rolls back (see Transaction.ModifiedIndexCatalogs).
        /// </summary>
        private void InvalidateIndexCatalog(Transaction transaction, Rdb rdb)
        {
            transaction.ModifiedIndexCatalogs.TryAdd(rdb.Path, rdb);
            InvalidateIndexCatalog(rdb);
        }

        internal void InvalidateIndexCatalog(Rdb rdb)
        {
            lock (_catalogCacheLock)
            {
                _catalogGeneration++;
                _catalogCache.Remove(rdb);
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
