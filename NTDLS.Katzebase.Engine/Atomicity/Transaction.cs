using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Instrumentation;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.Engine.Locking;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers;
using NTDLS.Katzebase.Parsers.Interfaces;
using NTDLS.Katzebase.PersistentTypes.Atomicity;
using NTDLS.Semaphore;
using RocksDbSharp;
using System.Collections.Concurrent;
using System.Text;
using static NTDLS.Katzebase.Api.KbConstants;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Atomicity
{
    /// <summary>
    /// A collection of reversable work and deferred IO.
    /// </summary>
    internal class Transaction
        : ITransaction, IDisposable
    {
        private readonly Lock _identityLock = new();

        // Dedup guards for the transaction atom log. Capped to avoid unbounded growth during
        // bulk operations; when capped, duplicate atoms may be written (harmless on rollback).
        private const int MaxAtomDeduplicationEntries = 10000;
        private readonly HashSet<string> _recordedReadObjectKeys = new HashSet<string>();
        private readonly HashSet<string> _recordedWriteObjectKeys = new HashSet<string>();
        private bool _writeTrackingCapped = false;
        private bool _readTrackingCapped = false;

        public string TopLevelOperation { get; set; } = string.Empty;
        public Guid Id { get; internal set; } = Guid.NewGuid();
        /// <summary>
        /// When we create the transaction log database, we split it into buvkets
        /// </summary>
        public List<KbQueryResultMessage> Messages { get; private set; } = new();
        public ulong ProcessId { get; private set; }
        public SessionState Session => _core.Sessions.ByProcessId(ProcessId);

        public DateTime StartTime { get; private set; }
        public bool IsDeadlocked { get; private set; }
        public InstrumentationTracker Instrumentation { get; private set; }
        public OptimisticSemaphore TransactionSemaphore { get; } = new();

        /// <summary>
        /// Whether the transaction was user created or not. The server implicitly creates lightweight transactions for everything.
        /// </summary>
        public bool IsUserCreated { get; set; }

        private readonly EngineCore _core;
        private readonly TransactionManager _transactionManager;
        private readonly PessimisticCriticalResource<Dictionary<KbTransactionWarning, HashSet<string>>> _warnings = new();

        public bool IsCommittedOrRolledBack { get; private set; } = false;
        public bool IsCancelled { get; private set; } = false;

        private long _referenceCount = 0;
        public long ReferenceCount
        {
            set => Interlocked.Exchange(ref _referenceCount, value);
            get => Interlocked.Read(ref _referenceCount);
        }

        #region Critical objects (Any object in this region must be locked for access).

        /// <summary>
        /// Write-cached objects that need to be flushed to disk upon commit.
        /// </summary>
        public OptimisticCriticalResource<DeferredDiskIO> DeferredIOs { get; private set; } = new();

        /// <summary>
        /// Files that have been read by the transaction. These will be placed into read
        /// cache and since they can be modified in memory, the cached items must be removed upon rollback.
        /// </summary>
        public OptimisticCriticalResource<HashSet<ReadForCacheItem>> FilesReadForCache { get; set; } = new();

        /// <summary>
        /// We keep a hash-set of locks granted to this transaction by the LockIntention.Key so that we
        ///     do not have to perform blocking or deadlock checks again for the life of this transaction.
        /// </summary>
        public OptimisticCriticalResource<HashSet<string>> GrantedLockCache { get; private set; }

        /// <summary>
        /// Outstanding lock-keys that are blocking this transaction.
        /// </summary>
        public OptimisticCriticalResource<List<ObjectLockKey>> BlockedByKeys { get; private set; }

        /// <summary>
        /// Lock if you need to read/write.
        /// All lock-keys that are currently held by the transaction.
        /// </summary>
        public OptimisticCriticalResource<List<ObjectLockKey>> HeldLockKeys { get; private set; }

        /// <summary>
        /// Lock if you need to read/write.
        /// Any temporary schemas that have been created in this transaction.
        /// </summary>
        public OptimisticCriticalResource<HashSet<string>> TemporarySchemas { get; private set; } = new();

        #endregion

        private readonly ConcurrentDictionary<string, WriteBatch> _rdbBatches =
            new(StringComparer.InvariantCultureIgnoreCase);

        internal WriteBatch AcquireRdbWriteBatch(string filePath)
        {
            return _rdbBatches.GetOrAdd(filePath, path =>
            {
                return new WriteBatch();
            });
        }

        #region Internal-system query utilities.

        /// <summary>
        /// Executes a query and returns the mapped object.
        /// Internal system usage only.
        /// </summary>
        internal IEnumerable<T> ExecuteQuery<T>(string queryText, object? userParameters = null) where T : new()
        {
            queryText = EmbeddedScripts.GetScriptOrLoadFile(queryText);

            var queries = StaticBatchParser.Parse(queryText, _core.GlobalConstants, userParameters.ToUserParametersInsensitiveDictionary());
            if (queries.Count > 1)
            {
                throw new KbMultipleRecordSetsException("Prepare batch resulted in more than one query.");
            }
            var results = _core.Query.ExecuteQuery(Session, queries[0]);
            if (queries.Count > 1)
            {
                throw new KbMultipleRecordSetsException();
            }

            if (results.Collection.Count == 0)
            {
                return new List<T>();
            }

            return results.Collection[0].MapTo<T>();
        }

        /// <summary>
        /// Executes a query and returns the collection
        /// Internal system usage only.
        /// </summary>
        internal KbQueryResultCollection ExecuteQuery(string queryText, object? userParameters = null)
        {
            queryText = EmbeddedScripts.GetScriptOrLoadFile(queryText);
            var results = new KbQueryResultCollection();
            Session.PushCurrentQuery(queryText);

            foreach (var query in StaticBatchParser.Parse(queryText, _core.GlobalConstants, userParameters.ToUserParametersInsensitiveDictionary()))
            {
                results.Add(_core.Query.ExecuteQuery(Session, query));
            }

            Session.PopCurrentQuery();

            return results;
        }

        /// <summary>
        /// Executes a query without a result.
        /// Internal system usage only.
        /// </summary>
        internal KbQueryResultCollection ExecuteNonQuery(string queryText, object? userParameters = null)
        {
            queryText = EmbeddedScripts.GetScriptOrLoadFile(queryText);
            var results = new KbQueryResultCollection();
            Session.PushCurrentQuery(queryText);

            foreach (var query in StaticBatchParser.Parse(queryText, _core.GlobalConstants, userParameters.ToUserParametersInsensitiveDictionary()))
            {
                results.Add(_core.Query.ExecuteQuery(Session, query));
            }

            Session.PopCurrentQuery();

            return results;
        }

        /// <summary>
        /// Executes a query and returns the first row and field object.
        /// Internal system usage only.
        /// </summary>
        internal T? ExecuteScalar<T>(string queryText, object? userParameters = null) //where T : new()
        {
            queryText = EmbeddedScripts.GetScriptOrLoadFile(queryText);

            var queries = StaticBatchParser.Parse(queryText, _core.GlobalConstants, userParameters.ToUserParametersInsensitiveDictionary());
            if (queries.Count > 1)
            {
                throw new KbMultipleRecordSetsException("Prepare batch resulted in more than one query.");
            }
            var results = _core.Query.ExecuteQuery(Session, queries[0]);
            if (queries.Count > 1)
            {
                throw new KbMultipleRecordSetsException();
            }

            if (results.Collection.Count == 0)
            {
                return default;
            }
            else if (results.Collection[0].Rows.Count == 0)
            {
                return default;
            }
            else if (results.Collection[0].Rows[0].Values.Count == 0)
            {
                return default;
            }

            return Converters.ConvertToNullable<T>(results.Collection[0].Value(0, 0));
        }

        #endregion

        public TransactionSnapshot Snapshot()
        {
            var snapshot = new TransactionSnapshot()
            {
                Id = Id,
                ProcessId = ProcessId,
                StartTime = StartTime,
                ReferenceCount = ReferenceCount,
                IsDeadlocked = IsDeadlocked,
                IsUserCreated = IsUserCreated,
                TopLevelOperation = TopLevelOperation,
                IsCommittedOrRolledBack = IsCommittedOrRolledBack,
                IsCancelled = IsCancelled
            };

            GrantedLockCache.DeadlockAvoidanceTryRead(10, _core.CancellationToken, (obj) => { snapshot.GrantedLockCache = new HashSet<string>(obj); });
            BlockedByKeys.DeadlockAvoidanceTryRead(10, _core.CancellationToken, (obj) => { snapshot.BlockedByKeys = obj.Select(o => o.Snapshot()).ToList(); });
            HeldLockKeys.DeadlockAvoidanceTryRead(10, _core.CancellationToken, (obj) => { snapshot.HeldLockKeys = obj.Select(o => o.Snapshot()).ToList(); });
            TemporarySchemas.DeadlockAvoidanceTryRead(10, _core.CancellationToken, (obj) => { snapshot.TemporarySchemas = new HashSet<string>(obj); });
            FilesReadForCache.DeadlockAvoidanceTryRead(10, _core.CancellationToken, (obj) => { snapshot.FilesReadForCache = new HashSet<ReadForCacheItem>(obj); });
            DeferredIOs.DeadlockAvoidanceTryRead(10, _core.CancellationToken, (obj) => { snapshot.DeferredIOs = obj.Snapshot(); });

            return snapshot;
        }

        /// <summary>
        /// A warning is different than a "message" warning. Warnings in this context
        ///     are used to report on query warnings such as nulls in documents.
        /// </summary>
        /// <param name="warning"></param>
        /// <param name="message"></param>
        public void AddWarning(KbTransactionWarning warning, string message = "")
        {
            switch (warning)
            {
                case KbTransactionWarning.FieldNotFound:
                    if (!Session.GetConnectionSetting(StateSetting.WarnMissingFields, false))
                    {
                        return;
                    }
                    break;
                case KbTransactionWarning.NullValuePropagation:
                    if (!Session.GetConnectionSetting(StateSetting.WarnNullPropagation, false))
                    {
                        return;
                    }
                    break;
            }

            _warnings.Use((warnings) =>
            {
                if (warnings.ContainsKey(warning) == false)
                {
                    var messages = new HashSet<string>();
                    if (string.IsNullOrEmpty(message) == false)
                    {
                        messages.Add(message);
                    }
                    warnings.Add(warning, messages);
                }
                else
                {
                    var obj = warnings[warning];
                    //No need to duplicate or add any blank messages.
                    if (string.IsNullOrEmpty(message) == false && obj.Any(o => o == message) == false)
                    {
                        warnings[warning].Add(message);
                    }
                }
            });
        }

        public Dictionary<KbTransactionWarning, HashSet<string>> CloneWarnings()
        {
            return _warnings.Use((warnings) =>
            {
                return warnings.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new HashSet<string>(kvp.Value)
                );
            });
        }

        public void AddMessage(string text, KbMessageType type)
            => Messages.Add(new KbQueryResultMessage(text, type));

        /// <summary>
        /// Sets the transaction as "deadlocked", rolls back the transaction and does health reporting.
        /// </summary>
        public void SetDeadlocked()
        {
            IsDeadlocked = true;
            Rollback();
            _core.Health.IncrementDiscrete(HealthCounterType.DeadlockCount);
        }

        private void ReleaseLocks()
        {
            GrantedLockCache.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Clear());

            HeldLockKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) =>
            {
                foreach (var key in obj)
                {
                    key.TurnInKey();
                }
            });
        }

        internal void ReleaseLock(ObjectLockKey objectLock)
        {
            GrantedLockCache.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Remove(objectLock.Key));

            HeldLockKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) =>
            {
                obj.Remove(objectLock);
            });
        }

        public void EnsureActive()
        {
            if (IsCancelled)
            {
                throw new KbTransactionCancelledException("Transaction was cancelled");
            }
            else if (IsDeadlocked)
            {
                throw new KbTransactionCancelledException("Transaction was deadlocked");
            }
            else if (IsCommittedOrRolledBack)
            {
                throw new KbTransactionCancelledException("Transaction was committed or rolled back.");
            }
        }

        #region IDisposable.

        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                //Rollback Transaction if its still open:
                if (IsUserCreated == false && IsCommittedOrRolledBack == false)
                {
                    Rollback();
                }
            }

            disposed = true;
        }

        #endregion

        #region Locking Helpers.

        public ObjectLockKey? LockSingleObject(LockOperation lockOperation, CacheKey targetKey)
        {
            if (lockOperation == LockOperation.Read && Session.GetConnectionSetting(StateSetting.ReadUncommitted, false))
            {
                lockOperation = LockOperation.Stability;
            }

            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var lockIntention = new ObjectLockIntention(this, targetKey, LockGranularity.Object, lockOperation);

                var ptLock = Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.Lock, $"Object:{lockIntention.Operation}");
                var result = _core.Locking.Acquire(this, lockIntention);
                ptLock?.StopAndAccumulate();

                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to acquire file lock.", ex);
                throw;
            }
        }

        /// <summary>
        /// Locks a single database schema path and all files (but not sub-schemas) that it contains.
        /// </summary>
        public ObjectLockKey? LockPath(LockOperation lockOperation, CacheKey targetKey)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var lockIntention = new ObjectLockIntention(this, targetKey, LockGranularity.Path, lockOperation);

                var ptLock = Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.Lock, $"Path:{lockIntention.Operation}");
                var result = _core.Locking.Acquire(this, lockIntention);
                ptLock?.StopAndAccumulate();

                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to acquire file lock.", ex);
                throw;
            }
        }

        /// <summary>
        /// Locks a path (which means the schema, sub-schema and all files beneath it).
        /// </summary>
        public ObjectLockKey? LockPathRecursive(LockOperation lockOperation, CacheKey targetKey)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var lockIntention = new ObjectLockIntention(this, targetKey, LockGranularity.PathRecursive, lockOperation);

                var ptLock = Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.Lock, $"Path:{lockIntention.Operation}");
                var result = _core.Locking.Acquire(this, lockIntention);
                ptLock?.StopAndAccumulate();

                return result;
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to acquire file lock.", ex);
                throw;
            }
        }

        #endregion

        public string TransactionPath
        {
            get
            {
                _core.EnsureNotNull();
                return Path.Combine(_core.Settings.TransactionDataPath, ProcessId.ToString());
            }
        }

        public string TransactionLogFilePath
            => TransactionPath + "\\" + TransactionActionsFile;

        public Transaction(EngineCore core, TransactionManager transactionManager, ulong processId, bool isRecovery)
        {
            _core = core;
            _transactionManager = transactionManager;

            bool enableInstrumentation = false;

            GrantedLockCache = new(core.LockManagementSemaphore);
            BlockedByKeys = new(core.LockManagementSemaphore);
            HeldLockKeys = new(core.LockManagementSemaphore);

            StartTime = DateTime.UtcNow;
            ProcessId = processId;

            DeferredIOs.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.SetCore(core));

            if (isRecovery == false)
            {
                var session = core.Sessions.ByProcessId(processId);

                enableInstrumentation = session.GetConnectionSetting(StateSetting.TraceWaitTimes, false);

                //Create a transaction log column family for this transaction:
                transactionManager.TxRdb.CreateColumnFamily(new RdbKey(Id));

                //We also need to put an entry in the identity column family so we can use it to serialize the atoms of the new transaction.
                _core.IO.PutNonTrackedRaw(transactionManager.TxRdb, KbColumnFamilyName.Identity, new RdbKey(Id), BitConverter.GetBytes(1L));
            }

            Instrumentation = new InstrumentationTracker(enableInstrumentation);
        }

        public long GetNextAtomSequence()
        {
            lock (_identityLock)
            {
                var bytes = _core.IO.GetNotTrackedRaw(_transactionManager.TxRdb, KbColumnFamilyName.Identity, new RdbKey(Id));
                var number = bytes == null ? 0L : BitConverter.ToInt64(bytes);
                number++;
                _core.IO.PutNonTrackedRaw(_transactionManager.TxRdb, KbColumnFamilyName.Identity, new RdbKey(Id), BitConverter.GetBytes(number));
                return number;
            }
        }

        #region Action Recorders.

        public void RecordKeyCreate(string rdbPath, KbColumnFamilyName columnFamily, RdbKey key, CacheKey targetKey)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptRecording = Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.AtomRecording);

                lock (_recordedWriteObjectKeys)
                {
                    if (!_writeTrackingCapped)
                    {
                        if (_recordedWriteObjectKeys.Contains(targetKey.Canonical))
                        {
                            return;
                        }
                        _recordedWriteObjectKeys.Add(targetKey.Canonical);
                        if (_recordedWriteObjectKeys.Count >= MaxAtomDeduplicationEntries)
                        {
                            _writeTrackingCapped = true;
                            _recordedWriteObjectKeys.Clear();
                            _recordedWriteObjectKeys.TrimExcess();
                        }
                    }
                }

                var atom = new Atom(ActionType.KeyCreate, GetNextAtomSequence(), rdbPath, columnFamily, key.Bytes, targetKey);

                var atomJson = JsonConvert.SerializeObject(atom);
                _core.IO.PutNonTrackedRaw(_transactionManager.TxRdb, new RdbKey(Id), new RdbKey(atom.Sequence), Encoding.UTF8.GetBytes(atomJson));
                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to record key creation for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordKeyDelete(string rdbPath, KbColumnFamilyName columnFamily, RdbKey key, CacheKey targetKey, byte[] originalData)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptRecording = Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.AtomRecording);

                DeferredIOs.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Remove(targetKey));

                lock (_recordedWriteObjectKeys)
                {
                    if (!_writeTrackingCapped)
                    {
                        if (_recordedWriteObjectKeys.Contains(targetKey.Canonical))
                        {
                            return;
                        }
                        _recordedWriteObjectKeys.Add(targetKey.Canonical);
                        if (_recordedWriteObjectKeys.Count >= MaxAtomDeduplicationEntries)
                        {
                            _writeTrackingCapped = true;
                            _recordedWriteObjectKeys.Clear();
                            _recordedWriteObjectKeys.TrimExcess();
                        }
                    }
                }

                var atom = new Atom(ActionType.KeyDelete, GetNextAtomSequence(), rdbPath, columnFamily, key.Bytes, targetKey)
                {
                    OriginalData = originalData
                };

                var atomJson = JsonConvert.SerializeObject(atom);
                _core.IO.PutNonTrackedRaw(_transactionManager.TxRdb, new RdbKey(Id), new RdbKey(atom.Sequence), Encoding.UTF8.GetBytes(atomJson));
                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to record key deletion for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordKeyRead(string rdbPath, KbColumnFamilyName columnFamily, RdbKey key, CacheKey targetKey)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptRecording = Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.AtomRecording);

                lock (_recordedReadObjectKeys)
                {
                    if (_readTrackingCapped)
                    {
                        return;
                    }
                    if (_recordedReadObjectKeys.Contains(targetKey.Canonical))
                    {
                        return;
                    }
                    _recordedReadObjectKeys.Add(targetKey.Canonical);
                    if (_recordedReadObjectKeys.Count >= MaxAtomDeduplicationEntries)
                    {
                        _readTrackingCapped = true;
                        _recordedReadObjectKeys.Clear();
                        _recordedReadObjectKeys.TrimExcess();
                    }
                }

                FilesReadForCache.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Add(new ReadForCacheItem(targetKey, key.Bytes)));

                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to record key read for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordKeyAlter(string rdbPath, KbColumnFamilyName columnFamily, RdbKey key, CacheKey targetKey, byte[] originalData)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptRecording = Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.AtomRecording);

                lock (_recordedWriteObjectKeys)
                {
                    if (!_writeTrackingCapped)
                    {
                        if (_recordedWriteObjectKeys.Contains(targetKey.Canonical))
                        {
                            return;
                        }
                        _recordedWriteObjectKeys.Add(targetKey.Canonical);
                        if (_recordedWriteObjectKeys.Count >= MaxAtomDeduplicationEntries)
                        {
                            _writeTrackingCapped = true;
                            _recordedWriteObjectKeys.Clear();
                            _recordedWriteObjectKeys.TrimExcess();
                        }
                    }
                }

                var atom = new Atom(ActionType.KeyAlter, GetNextAtomSequence(), rdbPath, columnFamily, key.Bytes, targetKey)
                {
                    OriginalData = originalData
                };

                var atomJson = JsonConvert.SerializeObject(atom);


                _core.IO.PutNonTrackedRaw(_transactionManager.TxRdb, new RdbKey(Id), new RdbKey(atom.Sequence), Encoding.UTF8.GetBytes(atomJson));
                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to record key alteration for process {ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        public void AddReference()
           => Interlocked.Increment(ref _referenceCount);

        /// <summary>
        /// This is safe to call on committed, rolled-back or canceled transactions.
        /// </summary>
        public void Rollback()
        {
            _core.EnsureNotNull();

            TransactionSemaphore.WriteAll([_transactionManager.CriticalSection], () =>
            {
                if (IsCommittedOrRolledBack)
                {
                    return;
                }

                IsCommittedOrRolledBack = true;
                IsCancelled = true;

                try
                {
                    var ptRollback = Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.Rollback);
                    try
                    {
                        var txCf = _transactionManager.TxRdb.GetColumnFamily(new RdbKey(Id));

                        using (var iterator = _transactionManager.TxRdb.NewIterator(txCf))
                        {
                            for (iterator.SeekToLast(); iterator.Valid(); iterator.Prev())
                            {
                                var record = JsonConvert.DeserializeObject<Atom>(iterator.StringValue());
                                if (record == null)
                                {
                                    LogManager.Warning($"Transaction atom is null for {ProcessId}");
                                    continue;
                                }

                                //We need to eject the rolled back item from the cache since its last known state has changed.
                                _core.Cache.Remove(record.CacheKey);

                                if (record.Action == ActionType.KeyCreate)
                                {
                                    try
                                    {
                                        var originalRdb = _core.IO.AcquireRdb(record.RdbPath.EnsureNotNull());
                                        var originalCf = originalRdb.GetColumnFamily(record.ColumnFamilyName);
                                        originalRdb.Remove(record.RdbKey, originalCf);
                                    }
                                    catch (Exception ex)
                                    {
                                        LogManager.Error($"Failed to remove key for transaction {ProcessId}.", ex);
                                    }
                                }
                                else if (record.Action == ActionType.KeyAlter || record.Action == ActionType.KeyDelete)
                                {
                                    try
                                    {
                                        var originalRdb = _core.IO.AcquireRdb(record.RdbPath.EnsureNotNull());
                                        var originalCf = originalRdb.GetColumnFamily(record.ColumnFamilyName);
                                        originalRdb.Put(record.RdbKey, record.OriginalData, originalCf);
                                    }
                                    catch (Exception ex)
                                    {
                                        LogManager.Error($"Failed to restore key for transaction {ProcessId}.", ex);
                                    }
                                }
                            }
                        } // Iterator closed before CleanupTransaction so the CF handle can be safely destroyed.

                        if (!_readTrackingCapped)
                        {
                            FilesReadForCache.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) =>
                            {
                                foreach (var file in obj)
                                {
                                    _core.Cache.Remove(file.CacheKey);
                                }
                            });
                        }

                        try
                        {
                            CleanupTransaction();
                        }
                        catch (Exception ex)
                        {
                            LogManager.Warning($"Failed to cleanup transaction log for process {ProcessId}: {ex.Message}");
                        }

                        _transactionManager.RemoveByProcessId(ProcessId);
                        DeleteTemporarySchemas();
                    }
                    finally
                    {
                        ReleaseLocks();
                        ptRollback?.StopAndAccumulate();
                        Instrumentation?.AddDiscreteMetric(InstrumentationTracker.DiscretePerformanceCounter.TransactionDuration, (DateTime.UtcNow - StartTime).TotalMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to rollback transaction for process {ProcessId}.", ex);
                    throw;
                }
            });
        }

        /// <summary>
        /// Dereferences a transaction, if the references fall to zero then the transaction should be disposed.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KbTransactionCancelledException"></exception>
        /// <exception cref="KbGenericException"></exception>
        public bool Commit()
        {
            _core.EnsureNotNull();

            return TransactionSemaphore.Write(() =>
            {
                if (IsCancelled)
                {
                    throw new KbTransactionCancelledException();
                }

                if (IsCommittedOrRolledBack)
                {
                    return true;
                }

                try
                {
                    var ptCommit = Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.Commit);
                    _referenceCount--;

                    if (_referenceCount == 0)
                    {
                        IsCommittedOrRolledBack = true;

                        try
                        {
                            DeferredIOs.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.CommitDeferredDiskIO());
                        }
                        finally
                        {
                            Exceptions.OnError(() => CleanupTransaction(),
                                (ex) => LogManager.Error($"Failed to cleanup transaction for process {ProcessId} during commit.", ex));

                            Exceptions.OnError(() => _transactionManager.RemoveByProcessId(ProcessId),
                                (ex) => LogManager.Error($"Failed to remove transaction from manager for process {ProcessId} during commit.", ex));

                            Exceptions.OnError(() => DeleteTemporarySchemas(),
                                (ex) => LogManager.Error($"Failed to delete temporary schemas for process {ProcessId} during commit.", ex));

                            Exceptions.OnError(() => ReleaseLocks(),
                                (ex) => LogManager.Error($"Failed to release locks for process {ProcessId} during commit.", ex));
                        }
                        ptCommit?.StopAndAccumulate();
                        Instrumentation?.AddDiscreteMetric(InstrumentationTracker.DiscretePerformanceCounter.TransactionDuration, (DateTime.UtcNow - StartTime).TotalMilliseconds);
                        return true;
                    }
                    else if (_referenceCount < 0)
                    {
                        throw new KbGenericException("Transaction reference count fell below zero.");
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to commit transaction for process {ProcessId}.", ex);
                    throw;
                }
                return false;
            });
        }

        private void DeleteTemporarySchemas()
        {
            _core.EnsureNotNull();

            TemporarySchemas.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) =>
            {
                if (obj.Count != 0)
                {
                    using var ephemeralTxRef = _core.Transactions.APIAcquire(Session);
                    foreach (var tempSchema in obj)
                    {
                        _core.Schemas.Drop(ephemeralTxRef.Transaction, tempSchema);
                    }
                    ephemeralTxRef.Commit();
                }
            });
        }

        private void CleanupTransaction()
        {
            _core.EnsureNotNull();

            try
            {
                var rdb = _core.IO.AcquireRdb(_core.Settings.TransactionDataPath);
                //Drop the transaction log column family for this transaction.
                rdb.DropColumnFamily(new RdbKey(Id));
                // Remove the identity counter for this transaction.
                rdb.Remove(new RdbKey(Id).Bytes, rdb.GetColumnFamily(KbColumnFamilyName.Identity));
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to cleanup transaction for process {ProcessId}.", ex);
                throw;
            }
        }
    }
}
