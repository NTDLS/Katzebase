using Newtonsoft.Json;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Engine.Locking;
using NTDLS.Katzebase.Engine.Trace;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Atomicity
{
    internal class Transaction : IDisposable
    {
        public LockIntention? CurrentLockIntention { get; set; }
        public string TopLevelOperation { get; set; } = string.Empty;
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Dictionary<KbTransactionWarning, HashSet<string>> Warnings { get; private set; } = new();
        public List<KbQueryResultMessage> Messages { get; private set; } = new();
        public HashSet<string> FilesReadForCache { get; set; } = new();
        public List<Atom> Atoms { get; private set; } = new();
        public ulong ProcessId { get; private set; }
        public DateTime StartTime { get; private set; }
        /// <summary>
        /// Outstanding lock-keys that are blocking this transaction.
        /// </summary>
        public List<ObjectLockKey> BlockedByKeys { get; private set; } = new();
        public bool IsDeadlocked { get; set; }
        public List<ObjectLockKey> HeldLockKeys { get; private set; } = new();
        public PerformanceTrace? PT { get; private set; } = null;
        public HashSet<string> TemporarySchemas { get; private set; } = new();

        /// <summary>
        /// Used for general locking, if any.
        /// </summary>
        public object SyncObject { get; private set; } = new object();

        /// <summary>
        /// We keep a hashset of locks granted to this transaction by the LockIntention.Key so that we
        ///     do not have to perform blocking or deadlock checks again for the life of this transaction.
        /// </summary>
        public HashSet<string> GrantedLockCache { get; private set; } = new HashSet<string>();

        /// <summary>
        /// Whether the transaction was user created or not. The server implicitly creates lightweight transactions for everyhting.
        /// </summary>
        public bool IsUserCreated { get; set; }
        public DeferredDiskIO DeferredIOs { get; private set; }

        private readonly Core _core;
        private TransactionManager? _transactionManager;
        private StreamWriter? _transactionLogHandle = null;

        public bool IsComittedOrRolledBack { get; private set; } = false;
        public bool IsCancelled { get; private set; } = false;

        private int _referenceCount = 0;
        public int ReferenceCount
        {
            set
            {
                _referenceCount = value;
            }
            get
            {
                lock (this)
                {
                    return _referenceCount;
                }
            }
        }

        public TransactionSnapshot Snapshot()
        {
            var snapshot = new TransactionSnapshot()
            {
                Id = Id,
                ProcessId = ProcessId,
                CurrentLockIntention = CurrentLockIntention,
                StartTime = StartTime,
                ReferenceCount = ReferenceCount,
                IsDeadlocked = IsDeadlocked,
                IsUserCreated = IsUserCreated,
                TopLevelOperation = TopLevelOperation,
                IsComittedOrRolledBack = IsComittedOrRolledBack,
                IsCancelled = IsCancelled
            };

            lock (GrantedLockCache) snapshot.GrantedLockCache = new HashSet<string>(GrantedLockCache);
            lock (BlockedByKeys) snapshot.BlockedByKeys = BlockedByKeys.Select(o => o.Snapshot()).ToList();
            lock (HeldLockKeys) snapshot.HeldLockKeys = HeldLockKeys.Select(o => o.Snapshot()).ToList();
            lock (TemporarySchemas) snapshot.TemporarySchemas = new HashSet<string>(TemporarySchemas);
            lock (FilesReadForCache) snapshot.FilesReadForCache = new HashSet<string>(FilesReadForCache);
            lock (DeferredIOs) snapshot.DeferredIOs = DeferredIOs.Snapshot();
            lock (Atoms) snapshot.Atoms = Atoms.Select(o => o.Snapshot()).ToList();

            return snapshot;
        }

        public void AddWarning(KbTransactionWarning warning, string message = "")
        {
            lock (Warnings)
            {
                if (Warnings.ContainsKey(warning) == false)
                {
                    var messages = new HashSet<string>();
                    if (string.IsNullOrEmpty(message) == false)
                    {
                        messages.Add(message);
                    }
                    Warnings.Add(warning, messages);
                }
                else
                {
                    var obj = Warnings[warning];
                    //No need to duplicate or blank any messages.
                    if (string.IsNullOrEmpty(message) == false && obj.Any(o => o == message) == false)
                    {
                        Warnings[warning].Add(message);
                    }
                }
            }
        }

        public void AddMessage(string text, KbMessageType type)
        {
            Messages.Add(new KbQueryResultMessage(text, type));
        }

        private void ReleaseLocks()
        {
            lock (GrantedLockCache)
            {
                GrantedLockCache.Clear();
            }

            lock (HeldLockKeys)
            {
                foreach (var key in HeldLockKeys)
                {
                    key.TurnInKey();
                }
            }
        }

        public void EnsureActive()
        {
            if (IsCancelled)
            {
                throw new KbTransactionCancelledException("The transaction was cancelled");
            }
            if (IsDeadlocked)
            {
                throw new KbTransactionCancelledException("The transaction was deadlocked");
            }
            if (IsComittedOrRolledBack)
            {
                throw new KbTransactionCancelledException("The transaction was comitted or rolled back.");
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
                if (IsUserCreated == false && IsComittedOrRolledBack == false)
                {
                    Rollback();
                }
            }

            disposed = true;
        }

        #endregion

        #region Locking Helpers.

        public void LockFile(LockOperation lockOperation, string diskpath)
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                lock (SyncObject)
                {
                    EnsureActive();

                    var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"File:{lockOperation}");

                    diskpath = diskpath.ToLower();

                    var lockIntention = new LockIntention(diskpath, LockGranularity.File, lockOperation);
                    _core.Locking.Locks.Acquire(this, lockIntention);
                    ptLock?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to acquire file lock.", ex);
                throw;
            }
        }

        public void LockDirectory(LockOperation lockOperation, string diskpath)
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                lock (SyncObject)
                {
                    EnsureActive();

                    var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"Directory:{lockOperation}");

                    diskpath = diskpath.ToLower();

                    var lockIntention = new LockIntention(diskpath, LockGranularity.Directory, lockOperation);
                    _core.Locking.Locks.Acquire(this, lockIntention);

                    ptLock?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to acquire file lock.", ex);
                throw;
            }
        }

        public void LockPath(LockOperation lockOperation, string diskpath)
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                lock (SyncObject)
                {
                    EnsureActive();

                    var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"Directory:{lockOperation}");

                    diskpath = diskpath.ToLower();

                    var lockIntention = new LockIntention(diskpath, LockGranularity.Path, lockOperation);
                    _core.Locking.Locks.Acquire(this, lockIntention);

                    ptLock?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to acquire file lock.", ex);
                throw;
            }
        }


        #endregion

        public void SetManager(TransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        public string TransactionPath
        {
            get
            {
                KbUtility.EnsureNotNull(_core);
                return Path.Combine(_core.Settings.TransactionDataPath, ProcessId.ToString());
            }
        }

        public string TransactionLogFilePath
        {
            get
            {
                return TransactionPath + "\\" + TransactionActionsFile;
            }
        }

        public Transaction(Core core, TransactionManager transactionManager, ulong processId, bool isRecovery)
        {
            _core = core;
            _transactionManager = transactionManager;

            StartTime = DateTime.UtcNow;
            ProcessId = processId;

            DeferredIOs = new DeferredDiskIO(core);

            if (isRecovery == false)
            {
                var session = core.Sessions.ByProcessId(processId);
                if (session.GetConnectionSetting(Sessions.SessionState.KbConnectionSetting.TraceWaitTimes) == 1)
                {
                    PT = new PerformanceTrace();
                }

                Directory.CreateDirectory(TransactionPath);

                _transactionLogHandle = new StreamWriter(TransactionLogFilePath)
                {
                    AutoFlush = true
                };

                KbUtility.EnsureNotNull(_transactionLogHandle);
            }
        }

        #region Action Recorders.

        private bool IsFileAlreadyRecorded(string filePath)
        {
            lock (Atoms)
            {
                return Atoms.Exists(o => o.Key == filePath.ToLower());
            }
        }

        public void RecordFileCreate(string filePath)
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                lock (SyncObject)
                {
                    EnsureActive();

                    var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);
                    lock (Atoms)
                    {
                        if (IsFileAlreadyRecorded(filePath))
                        {
                            return;
                        }

                        var atom = new Atom(ActionType.FileCreate, filePath)
                        {
                            Sequence = Atoms.Count
                        };

                        Atoms.Add(atom);

                        KbUtility.EnsureNotNull(_transactionLogHandle);

                        _transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
                    }

                    ptRecording?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to record file creation for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordDirectoryCreate(string path)
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                lock (SyncObject)
                {
                    EnsureActive();

                    var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);
                    lock (Atoms)
                    {
                        if (IsFileAlreadyRecorded(path))
                        {
                            return;
                        }

                        var atom = new Atom(ActionType.DirectoryCreate, path)
                        {
                            Sequence = Atoms.Count
                        };

                        Atoms.Add(atom);

                        KbUtility.EnsureNotNull(_transactionLogHandle);

                        _transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
                    }
                    ptRecording?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to record file creation for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordPathDelete(string diskPath)
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                lock (SyncObject)
                {
                    EnsureActive();

                    var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                    lock (DeferredIOs) DeferredIOs.RemoveItemsWithPrefix(diskPath);

                    lock (Atoms)
                    {
                        if (IsFileAlreadyRecorded(diskPath))
                        {
                            return;
                        }

                        string backupPath = Path.Combine(TransactionPath, Guid.NewGuid().ToString());
                        Directory.CreateDirectory(backupPath);
                        Helpers.CopyDirectory(diskPath, backupPath);

                        var atom = new Atom(ActionType.DirectoryDelete, diskPath)
                        {
                            BackupPath = backupPath,
                            Sequence = Atoms.Count
                        };

                        Atoms.Add(atom);

                        KbUtility.EnsureNotNull(_transactionLogHandle);

                        _transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
                    }
                    ptRecording?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to record file deletion for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordFileDelete(string filePath)
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                lock (SyncObject)
                {
                    EnsureActive();

                    var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                    lock (DeferredIOs) DeferredIOs.Remove(filePath);

                    lock (Atoms)
                    {
                        if (IsFileAlreadyRecorded(filePath))
                        {
                            return;
                        }

                        string backupPath = Path.Combine(TransactionPath, Guid.NewGuid() + ".bak");
                        File.Copy(filePath, backupPath);

                        var atom = new Atom(ActionType.FileDelete, filePath)
                        {
                            BackupPath = backupPath,
                            Sequence = Atoms.Count
                        };

                        Atoms.Add(atom);

                        KbUtility.EnsureNotNull(_transactionLogHandle);

                        _transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
                    }
                    ptRecording?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to record file deletion for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordFileRead(string filePath)
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                lock (SyncObject)
                {
                    EnsureActive();

                    var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                    lock (FilesReadForCache) FilesReadForCache.Add(filePath);
                    ptRecording?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to record file read for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordFileAlter(string filePath)
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                lock (SyncObject)
                {
                    EnsureActive();

                    var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                    lock (Atoms)
                    {
                        if (IsFileAlreadyRecorded(filePath))
                        {
                            return;
                        }

                        string backupPath = Path.Combine(TransactionPath, Guid.NewGuid() + ".bak");
                        File.Copy(filePath, backupPath);

                        var atom = new Atom(ActionType.FileAlter, filePath)
                        {
                            BackupPath = backupPath,
                            Sequence = Atoms.Count
                        };

                        Atoms.Add(atom);

                        KbUtility.EnsureNotNull(_transactionLogHandle);

                        _transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
                    }
                    ptRecording?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to record file alteration for process {ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        public void AddReference()
        {
            lock (this)
            {
                _referenceCount++;
            }
        }

        public void Rollback()
        {
            KbUtility.EnsureNotNull(_core);

            lock (SyncObject)
            {
                if (IsComittedOrRolledBack)
                {
                    return;
                }

                IsComittedOrRolledBack = true;
                IsCancelled = true;

                try
                {
                    var ptRollback = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Rollback);
                    try
                    {
                        var rollbackActions = Atoms.OrderByDescending(o => o.Sequence);

                        foreach (var record in rollbackActions)
                        {
                            //We need to eject the rolled back item from the cache since its last known state has changed.
                            _core.Cache.Remove(record.OriginalPath);

                            if (record.Action == ActionType.FileCreate)
                            {
                                try
                                {
                                    if (File.Exists(record.OriginalPath))
                                    {
                                        File.Delete(record.OriginalPath);
                                    }
                                }
                                catch
                                {
                                    //Discard.
                                }
                                Helpers.RemoveDirectoryIfEmpty(Path.GetDirectoryName(record.OriginalPath));
                            }
                            else if (record.Action == ActionType.FileAlter || record.Action == ActionType.FileDelete)
                            {
                                var diskPath = Path.GetDirectoryName(record.OriginalPath);

                                KbUtility.EnsureNotNull(diskPath);
                                KbUtility.EnsureNotNull(record.BackupPath);

                                Directory.CreateDirectory(diskPath);
                                File.Copy(record.BackupPath, record.OriginalPath, true);
                            }
                            else if (record.Action == ActionType.DirectoryCreate)
                            {
                                if (Directory.Exists(record.OriginalPath))
                                {
                                    Directory.Delete(record.OriginalPath, false);
                                }
                            }
                            else if (record.Action == ActionType.DirectoryDelete)
                            {
                                KbUtility.EnsureNotNull(record.BackupPath);
                                Helpers.CopyDirectory(record.BackupPath, record.OriginalPath);
                            }
                        }

                        lock (FilesReadForCache)
                        {
                            foreach (var file in FilesReadForCache)
                            {
                                //Un-cache files that we have read too. These might just be persistent in cache and never written and can affect state.
                                _core.Cache.Remove(file);
                            }
                        }

                        try
                        {
                            CleanupTransaction();
                        }
                        catch
                        {
                            //Discard.
                        }

                        _transactionManager?.RemoveByProcessId(ProcessId);
                        DeleteTemporarySchemas();
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        ReleaseLocks();
                    }

                    ptRollback?.StopAndAccumulate();
                    PT?.AddDescreteMetric(PerformanceTrace.PerformanceTraceDescreteMetricType.TransactionDuration, (DateTime.UtcNow - StartTime).TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    _core.Log.Write($"Failed to rollback transaction for process {ProcessId}.", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Dereferecnes a transaction, if the references fall to zero then the transaction should be disposed.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KbTransactionCancelledException"></exception>
        /// <exception cref="KbGenericException"></exception>
        public bool Commit()
        {
            KbUtility.EnsureNotNull(_core);

            lock (SyncObject)
            {
                if (IsCancelled)
                {
                    throw new KbTransactionCancelledException();
                }

                if (IsComittedOrRolledBack)
                {
                    return true;
                }

                try
                {
                    var ptCommit = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Commit);
                    lock (this)
                    {
                        _referenceCount--;

                        if (_referenceCount == 0)
                        {
                            IsComittedOrRolledBack = true;

                            try
                            {
                                lock (DeferredIOs) DeferredIOs.CommitDeferredDiskIO();
                                CleanupTransaction();
                                _transactionManager?.RemoveByProcessId(ProcessId);
                                DeleteTemporarySchemas();
                            }
                            catch
                            {
                                throw;
                            }
                            finally
                            {
                                ReleaseLocks();
                            }
                            ptCommit?.StopAndAccumulate();
                            PT?.AddDescreteMetric(PerformanceTrace.PerformanceTraceDescreteMetricType.TransactionDuration, (DateTime.UtcNow - StartTime).TotalMilliseconds);
                            return true;
                        }
                        else if (_referenceCount < 0)
                        {
                            throw new KbGenericException("Transaction reference count fell below zero.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _core.Log.Write($"Failed to commit transaction for process {ProcessId}.", ex);
                    throw;
                }
            }

            return false;
        }

        private void DeleteTemporarySchemas()
        {
            KbUtility.EnsureNotNull(_core);

            lock (TemporarySchemas)
            {
                if (TemporarySchemas.Any())
                {
                    using (var ephemeralTxRef = _core.Transactions.Acquire(ProcessId))
                    {
                        foreach (var tempSchema in TemporarySchemas)
                        {
                            _core.Schemas.Drop(ephemeralTxRef.Transaction, tempSchema);
                        }
                        ephemeralTxRef.Commit();
                    }
                }
            }
        }

        private void CleanupTransaction()
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                if (_transactionLogHandle != null)
                {
                    _transactionLogHandle.Close();
                    _transactionLogHandle.Dispose();
                    _transactionLogHandle = null;
                }

                foreach (var record in Atoms)
                {
                    //Delete all the backup files.
                    if (record.Action == ActionType.FileAlter || record.Action == ActionType.FileDelete)
                    {
                        KbUtility.EnsureNotNull(record.BackupPath);
                        File.Delete(record.BackupPath);
                    }
                    else if (record.Action == ActionType.DirectoryDelete)
                    {
                        KbUtility.EnsureNotNull(record.BackupPath);
                        Directory.Delete(record.BackupPath, true);
                    }
                }

                File.Delete(TransactionLogFilePath);
                Directory.Delete(TransactionPath, true);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to cleanup transaction for process {ProcessId}.", ex);
                throw;
            }
        }
    }
}
