using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.PersistentTypes.Schema;
using System.Diagnostics;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to documents.
    /// </summary>
    public class DocumentManager
    {
        private readonly EngineCore _core;
        private readonly object[] _identityLocks = Enumerable.Range(0, 256).Select(_ => new object()).ToArray();

        internal DocumentQueryHandlers QueryHandlers { get; private set; }
        public DocumentAPIHandlers APIHandlers { get; private set; }

        internal DocumentManager(EngineCore core)
        {
            _core = core;

            try
            {
                QueryHandlers = new DocumentQueryHandlers(core);
                APIHandlers = new DocumentAPIHandlers(core);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate document manager.", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to read a document we do it here.
        /// Allows for returning null if the document doesn't exist;
        /// </summary>
        internal PhysicalDocument? AcquireDocumentVirtual(
            Transaction transaction, Rdb rdb, uint documentId, LockOperation lockOp, bool populateCache = true)
        {
            try
            {
                return _core.IO.GetPBuf<PhysicalDocument>(transaction, rdb, KbColumnFamilyName.Documents, new RdbKey(documentId), lockOp, populateCache);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to read a document we do it here.
        /// </summary>
        internal PhysicalDocument AcquireDocument(
            Transaction transaction, Rdb rdb, uint documentId, LockOperation lockOp, bool populateCache = true)
        {
            try
            {
                return _core.IO.GetPBuf<PhysicalDocument>(transaction, rdb, KbColumnFamilyName.Documents, new RdbKey(documentId), lockOp, populateCache)
                    ?? throw new Exception($"Document with ID [{documentId}] does not exist in store [{rdb.Path}].");
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        internal HashSet<uint> AcquireDocumentPointers(
            Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockOp, int? maxCount = null)
        {
            try
            {
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);

                var documentPointers = new HashSet<uint>();

                using var iterator = rdb.NewIterator(KbColumnFamilyName.Documents);
                for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
                {
                    if (maxCount != null && documentPointers.Count >= maxCount.Value)
                    {
                        break;
                    }
                    documentPointers.Add(RdbKey.ConvertToUint(iterator.Key()));
                }

                return documentPointers;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Streams all documents in a schema as (documentId, document) pairs via a single sequential
        /// iterator scan over the Documents column family.  This is significantly faster than calling
        /// AcquireDocumentPointers followed by individual AcquireDocument calls because RocksDB's
        /// sequential iterator benefits from block prefetching, whereas N individual Gets each traverse
        /// the LSM tree independently with no prefetch benefit.
        ///
        /// If deferred IO is enabled and the current transaction has a pending (uncommitted) write for
        /// a given key, that in-memory version is yielded instead of the on-disk value so the caller
        /// always sees the transaction's own writes.
        /// </summary>
        internal IEnumerable<(uint DocumentId, PhysicalDocument Document)> ScanDocuments(
            Transaction transaction, PhysicalSchema physicalSchema, LockOperation lockOp)
        {
            var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
            var documentsCF = rdb.GetColumnFamily(KbColumnFamilyName.Documents);

            using var iterator = rdb.NewIterator(documentsCF);
            for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
            {
                var keyBytes = iterator.Key();
                uint documentId = RdbKey.ConvertToUint(keyBytes);
                var cacheKey = CacheManager.MakeCacheKey(physicalSchema.DocumentsFilePath(), KbColumnFamilyName.Documents, new RdbKey(keyBytes));

                transaction.RecordKeyRead(new RdbKey(keyBytes), cacheKey);

                PhysicalDocument? document = null;

                // If this transaction has a pending write for this key, use that version so the
                // caller sees its own uncommitted inserts/updates rather than the stale disk value.
                if (_core.Settings.DeferredIOEnabled)
                {
                    document = transaction.DeferredIOs.ReadNullable((dio) =>
                    {
                        var ptDeferred = transaction.Instrumentation.CreateToken(PerformanceCounter.DeferredRead);
                        bool wasDeferred = dio.GetDeferredDiskIO<PhysicalDocument>(cacheKey, out var deferred);
                        ptDeferred?.StopAndAccumulate();
                        return wasDeferred ? deferred : null;
                    });
                }

                if (document == null)
                {
                    document = transaction.Instrumentation.Measure(PerformanceCounter.Deserialize, () =>
                    {
                        using var ms = new MemoryStream(iterator.Value());
                        return ProtoBuf.Serializer.Deserialize<PhysicalDocument>(ms);
                    });
                }

                if (document != null)
                    yield return (documentId, document);
            }
        }

        /// <summary>
        /// Fixed memory, low contention at 256 buckets, no cleanup needed. Two different paths can theoretically share
        /// a bucket (hash collision) and serialize against each other, but it's rare and harmless — correctness is maintained either way.
        /// </summary>
        private object GetIdentityLock(string path) => _identityLocks[Math.Abs(path.GetHashCode()) % _identityLocks.Length];

        public uint GetNextIdentity(PhysicalSchema physicalSchema)
        {
            lock (GetIdentityLock(physicalSchema.SchemaFilePath()))
            {
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                var bytes = _core.IO.GetNotTrackedRaw(rdb, KbColumnFamilyName.Identity, new RdbKey(PrimaryIdentityKey));
                var identity = bytes == null ? 0U : BitConverter.ToUInt32(bytes);
                identity++;
                _core.IO.PutNonTrackedRaw(rdb, KbColumnFamilyName.Identity, new RdbKey(PrimaryIdentityKey), BitConverter.GetBytes(identity));
                return identity;
            }
        }

        public uint GetCurrentIdentity(PhysicalSchema physicalSchema)
        {
            lock (GetIdentityLock(physicalSchema.SchemaFilePath()))
            {
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                var bytes = _core.IO.GetNotTrackedRaw(rdb, KbColumnFamilyName.Identity, new RdbKey(PrimaryIdentityKey));
                var identity = bytes == null ? 0U : BitConverter.ToUInt32(bytes);
                return identity;
            }
        }

        /// <summary>
        /// When we want to create a document, this is where we do it - no exceptions.
        /// </summary>
        internal uint InsertDocument(Transaction transaction, PhysicalSchema physicalSchema, string pageContent)
        {
            try
            {
                uint physicalDocumentId = GetNextIdentity(physicalSchema);

                var physicalDocument = new PhysicalDocument(pageContent)
                {
                    CreatedUTC = DateTime.UtcNow,
                    ModifiedUTC = DateTime.UtcNow,
                };

                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);
                _core.IO.PutPBuf(transaction, rdb, KbColumnFamilyName.Documents, new RdbKey(physicalDocumentId), physicalDocument);

                //Update all of the indexes that reference the document.
                _core.Indexes.InsertDocumentIntoIndexes(transaction, physicalSchema, physicalDocument, physicalDocumentId);

                return physicalDocumentId;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to update multiple documents in the same schema, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="physicalSchema"></param>
        /// <param name="updatedDocuments">List of document pointers and their new content.</param>
        internal void UpdateDocuments(Transaction transaction, PhysicalSchema physicalSchema,
            Dictionary<uint, KbInsensitiveDictionary<string?>> updatedDocuments, bool populateCache = true)
        {
            try
            {
                var indexingDocuments = new Dictionary<uint, PhysicalDocument>();
                var modifiedFieldNames = new HashSet<string>();

                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);

                foreach (var updatedDocument in updatedDocuments)
                {
                    var physicalDocument = _core.IO.GetPBuf<PhysicalDocument>(transaction, rdb, KbColumnFamilyName.Documents, new RdbKey(updatedDocument.Key), LockOperation.Write, populateCache)
                        ?? throw new Exception($"Document with ID [{updatedDocument.Key}] does not exist in schema [{physicalSchema.Name}].");

                    physicalDocument.ModifiedUTC = DateTime.UtcNow;

                    //Update all of the modified values into the document:
                    foreach (var updatedValue in updatedDocument.Value)
                    {
                        physicalDocument.Elements[updatedValue.Key] = updatedValue.Value;
                        modifiedFieldNames.Add(updatedValue.Key);
                    }

                    //Keep track of the modified physical documents for indexing:
                    indexingDocuments.Add(updatedDocument.Key, physicalDocument);

                    //Save the document page:
                    _core.IO.PutPBuf(transaction, rdb, KbColumnFamilyName.Documents, new RdbKey(updatedDocument.Key), physicalDocument);
                }

                //var modifiedFieldNames = documentContent.Select(o=>o.Value);

                //Update all of the indexes that reference the document.
                _core.Indexes.UpdateDocumentsIntoIndexes(transaction, physicalSchema, indexingDocuments, modifiedFieldNames);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }

        /// <summary>
        /// When we want to update a document, this is where we do it - no exceptions.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        internal void DeleteDocuments(Transaction transaction, PhysicalSchema physicalSchema, IEnumerable<uint> documentIds)
        {
            try
            {
                var rdb = _core.IO.AcquireDocumentsRdb(physicalSchema);

                foreach (var documentId in documentIds)
                {
                    _core.IO.DeleteKey(transaction, rdb, KbColumnFamilyName.Documents, new RdbKey(documentId));

                }

                //Update all of the indexes that reference the documents.
                _core.Indexes.RemoveDocumentsFromIndexes(transaction, physicalSchema, documentIds);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}].", ex);
                throw;
            }
        }
    }
}
