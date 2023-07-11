using Katzebase.Engine.Atomicity.Management;
using Katzebase.Engine.IO;
using Katzebase.Engine.Library;
using Katzebase.Engine.Locking;
using Katzebase.Engine.Trace;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Newtonsoft.Json;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Atomicity
{
    internal class Transaction : IDisposable
    {
        public List<Atom> Atoms = new();
        public ulong ProcessId { get; set; }
        public DateTime StartTime { get; set; }
        public List<ulong> BlockedBy { get; set; }
        public bool IsDeadlocked { get; set; }
        public List<ObjectLockKey>? HeldLockKeys { get; set; }
        public PerformanceTrace? PT { get; private set; } = null;
        public HashSet<string> TemporarySchemas { get; set; } = new();

        /// <summary>
        /// Used for general locking, if any.
        /// </summary>
        public object SyncObject { get; private set; } = new object();

        /// <summary>
        /// We keep a hashset of locks granted to this transaction by the LockIntention.Key so that we
        ///     do not have to perform blocking or deadlock checks again for the life of this transaction.
        /// </summary>
        public HashSet<string> GrantedLockCache { get; set; } = new HashSet<string>();

        /// <summary>
        /// Whether the transaction was user created or not. The server implicitly creates lightweight transactions for everyhting.
        /// </summary>
        public bool IsUserCreated { get; set; }
        public DeferredDiskIO? DeferredIOs { get; set; }

        private readonly Core core;
        private TransactionManager transactionManager;
        private StreamWriter? transactionLogHandle = null;
        public bool IsComittedOrRolledBack { get; private set; } = false;
        public bool IsCancelled { get; private set; } = false;

        private int referenceCount = 0;
        public int ReferenceCount
        {
            set
            {
                referenceCount = value;
            }
            get
            {
                lock (this)
                {
                    return referenceCount;
                }
            }
        }

        internal List<ulong> CloneBlocks()
        {
            lock (CentralCriticalSections.AcquireLock)
            {
                var clone = new List<ulong>();
                clone.AddRange(this.BlockedBy);
                return clone;
            }
        }

        private void ReleaseLocks()
        {
            if (HeldLockKeys != null)
            {
                lock (HeldLockKeys)
                {
                    foreach (var key in HeldLockKeys)
                    {
                        key.TurnInKey();
                    }
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
            try
            {
                lock (SyncObject)
                {
                    EnsureActive();

                    var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"File:{lockOperation}");

                    diskpath = diskpath.ToLower();

                    KbUtility.EnsureNotNull(HeldLockKeys);

                    lock (HeldLockKeys)
                    {
                        var lockIntention = new LockIntention(diskpath, LockType.File, lockOperation);
                        core.Locking.Locks.Acquire(this, lockIntention);
                    }
                    ptLock?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to acquire file lock.", ex);
                throw;
            }
        }

        public void LockDirectory(LockOperation lockOperation, string diskpath)
        {
            try
            {
                lock (SyncObject)
                {
                    EnsureActive();

                    var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"Directory:{lockOperation}");

                    diskpath = diskpath.ToLower();

                    KbUtility.EnsureNotNull(HeldLockKeys);

                    lock (HeldLockKeys)
                    {
                        var lockIntention = new LockIntention(diskpath, LockType.Directory, lockOperation);
                        core.Locking.Locks.Acquire(this, lockIntention);
                    }

                    ptLock?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to acquire file lock.", ex);
                throw;
            }
        }

        #endregion

        public void SetManager(TransactionManager transactionManager)
        {
            this.transactionManager = transactionManager;
        }

        public string TransactionPath
        {
            get
            {
                return Path.Combine(core.Settings.TransactionDataPath, ProcessId.ToString());
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
            this.core = core;
            StartTime = DateTime.UtcNow;
            ProcessId = processId;
            this.transactionManager = transactionManager;
            BlockedBy = new List<ulong>();

            if (isRecovery == false)
            {
                var session = core.Sessions.ByProcessId(processId);
                if (session.GetConnectionSetting(Sessions.SessionState.KbConnectionSetting.TraceWaitTimes) == 1)
                {
                    PT = new PerformanceTrace();
                }

                HeldLockKeys = new List<ObjectLockKey>();
                DeferredIOs = new DeferredDiskIO(core);

                Directory.CreateDirectory(TransactionPath);

                transactionLogHandle = new StreamWriter(TransactionLogFilePath)
                {
                    AutoFlush = true
                };

                KbUtility.EnsureNotNull(transactionLogHandle);
            }
        }

        #region Action Recorders.

        private bool IsFileAlreadyRecorded(string filePath) => Atoms.Exists(o => o.Key == filePath.ToLower());

        public void RecordFileCreate(string filePath)
        {
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

                        KbUtility.EnsureNotNull(transactionLogHandle);

                        transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
                    }

                    ptRecording?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to record file creation for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordDirectoryCreate(string path)
        {
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

                        KbUtility.EnsureNotNull(transactionLogHandle);

                        transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
                    }
                    ptRecording?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to record file creation for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordPathDelete(string diskPath)
        {
            try
            {
                lock (SyncObject)
                {
                    EnsureActive();

                    var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

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

                        KbUtility.EnsureNotNull(transactionLogHandle);

                        transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
                    }
                    ptRecording?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to record file deletion for for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordFileDelete(string filePath)
        {
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

                        var atom = new Atom(ActionType.FileDelete, filePath)
                        {
                            BackupPath = backupPath,
                            Sequence = Atoms.Count
                        };

                        Atoms.Add(atom);

                        KbUtility.EnsureNotNull(transactionLogHandle);

                        transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
                    }
                    ptRecording?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to record file deletion for for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordFileAlter(string filePath)
        {
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

                        KbUtility.EnsureNotNull(transactionLogHandle);

                        transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
                    }
                    ptRecording?.StopAndAccumulate();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to record file alteration for for process {ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        public void AddReference()
        {
            lock (this)
            {
                referenceCount++;
            }
        }

        public void Rollback()
        {
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
                            core.Cache.Remove(record.OriginalPath);

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

                        try
                        {
                            CleanupTransaction();
                        }
                        catch
                        {
                            //Discard.
                        }

                        transactionManager.RemoveByProcessId(ProcessId);
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
                    core.Log.Write($"Failed to rollback transaction for for process {ProcessId}.", ex);
                    throw;
                }
            }
        }

        public void Commit()
        {
            lock (SyncObject)
            {
                if (IsCancelled)
                {
                    throw new KbTransactionCancelledException();
                }

                if (IsComittedOrRolledBack)
                {
                    return;
                }

                try
                {
                    var ptCommit = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Commit);
                    lock (this)
                    {
                        referenceCount--;

                        if (referenceCount == 0)
                        {
                            IsComittedOrRolledBack = true;

                            try
                            {
                                KbUtility.EnsureNotNull(DeferredIOs);
                                DeferredIOs.CommitDeferredDiskIO();
                                CleanupTransaction();
                                transactionManager.RemoveByProcessId(ProcessId);
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
                        }
                        else if (referenceCount < 0)
                        {
                            throw new KbGenericException("Transaction reference count fell below zero.");
                        }
                    }

                    ptCommit?.StopAndAccumulate();

                    PT?.AddDescreteMetric(PerformanceTrace.PerformanceTraceDescreteMetricType.TransactionDuration, (DateTime.UtcNow - StartTime).TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    core.Log.Write($"Failed to commit transaction for for process {ProcessId}.", ex);
                    throw;
                }
            }
        }

        private void DeleteTemporarySchemas()
        {
            if (TemporarySchemas.Any())
            {
                using (var ephemeralTx = core.Transactions.Acquire(ProcessId))
                {
                    foreach (var tempSchema in TemporarySchemas)
                    {
                        core.Schemas.Drop(ephemeralTx, tempSchema);
                    }
                    ephemeralTx.Commit();
                }
            }
        }

        private void CleanupTransaction()
        {
            try
            {
                if (transactionLogHandle != null)
                {
                    transactionLogHandle.Close();
                    transactionLogHandle.Dispose();
                    transactionLogHandle = null;
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
                core.Log.Write($"Failed to cleanup transaction for for process {ProcessId}.", ex);
                throw;
            }
        }
    }
}
