using Katzebase.Engine.KbLib;
using Katzebase.Engine.Locking;
using Katzebase.Engine.Trace;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Newtonsoft.Json;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Transactions
{
    internal class Transaction
    {
        public List<ReversibleAction> ReversibleActions = new List<ReversibleAction>();
        public ulong ProcessId { get; set; }
        public DateTime StartTime { get; set; }
        public List<ulong> BlockedBy { get; set; }
        public bool IsDeadlocked { get; set; }
        public List<ObjectLockKey>? HeldLockKeys { get; set; }
        public PerformanceTrace? PT { get; private set; } = null;
        
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

        private Core core;
        private TransactionManager transactionManager;
        private StreamWriter? transactionLogHandle = null;

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

        #region Locking Helpers.

        public void LockFile(LockOperation lockOperation, string diskpath)
        {
            try
            {
                var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"File:{lockOperation}");

                diskpath = diskpath.ToLower();

                Utility.EnsureNotNull(HeldLockKeys);

                lock (HeldLockKeys)
                {
                    var lockIntention = new LockIntention(diskpath, LockType.File, lockOperation);
                    core.Locking.Locks.Acquire(this, lockIntention);
                }
                ptLock?.StopAndAccumulate();
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
                var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"Directory:{lockOperation}");

                diskpath = diskpath.ToLower();

                Utility.EnsureNotNull(HeldLockKeys);

                lock (HeldLockKeys)
                {
                    var lockIntention = new LockIntention(diskpath, LockType.Directory, lockOperation);
                    core.Locking.Locks.Acquire(this, lockIntention);
                }

                ptLock?.StopAndAccumulate();
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
                return Path.Combine(core.settings.TransactionDataPath, ProcessId.ToString());
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
            this.StartTime = DateTime.UtcNow;
            this.ProcessId = processId;
            this.transactionManager = transactionManager;
            this.BlockedBy = new List<ulong>();

            if (isRecovery == false)
            {
                var session = core.Sessions.ByProcessId(processId);
                if (session.TraceWaitTimesEnabled)
                {
                    PT = new PerformanceTrace();
                }

                this.HeldLockKeys = new List<ObjectLockKey>();
                this.DeferredIOs = new DeferredDiskIO(core);

                Directory.CreateDirectory(TransactionPath);

                this.transactionLogHandle = new StreamWriter(TransactionLogFilePath)
                {
                    AutoFlush = true
                };
            }
        }

        #region Action Recorders.

        private bool IsFileAlreadyRecorded(string filePath)
        {
            filePath = Helpers.RemoveModFileName(filePath.ToLower());
            return ReversibleActions.Exists(o => o.OriginalPath == filePath);
        }

        public void RecordFileCreate(string filePath)
        {
            try
            {
                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);
                lock (ReversibleActions)
                {
                    if (IsFileAlreadyRecorded(filePath))
                    {
                        return;
                    }

                    var reversibleAction = new ReversibleAction(ActionType.FileCreate, filePath.ToLower())
                    {
                        Sequence = ReversibleActions.Count
                    };

                    ReversibleActions.Add(reversibleAction);

                    Utility.EnsureNotNull(transactionLogHandle);

                    this.transactionLogHandle.WriteLine(JsonConvert.SerializeObject(reversibleAction));
                }

                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to record file creation for process {this.ProcessId}.", ex);
                throw;
            }
        }

        public void RecordDirectoryCreate(string path)
        {
            try
            {
                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);
                lock (ReversibleActions)
                {
                    if (IsFileAlreadyRecorded(path))
                    {
                        return;
                    }

                    var reversibleAction = new ReversibleAction(ActionType.DirectoryCreate, path.ToLower())
                    {
                        Sequence = ReversibleActions.Count
                    };

                    ReversibleActions.Add(reversibleAction);

                    Utility.EnsureNotNull(transactionLogHandle);

                    this.transactionLogHandle.WriteLine(JsonConvert.SerializeObject(reversibleAction));
                }
                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to record file creation for process {this.ProcessId}.", ex);
                throw;
            }
        }

        public void RecordPathDelete(string diskPath)
        {
            try
            {
                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                lock (ReversibleActions)
                {
                    if (IsFileAlreadyRecorded(diskPath))
                    {
                        return;
                    }

                    string backupPath = Path.Combine(TransactionPath, Guid.NewGuid().ToString());
                    Directory.CreateDirectory(backupPath);
                    Helpers.CopyDirectory(diskPath, backupPath);

                    var reversibleAction = new ReversibleAction(ActionType.DirectoryDelete, diskPath.ToLower())
                    {
                        BackupPath = backupPath,
                        Sequence = ReversibleActions.Count
                    };

                    ReversibleActions.Add(reversibleAction);

                    Utility.EnsureNotNull(transactionLogHandle);

                    this.transactionLogHandle.WriteLine(JsonConvert.SerializeObject(reversibleAction));
                }
                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to record file deletion for for process {this.ProcessId}.", ex);
                throw;
            }
        }

        public void RecordFileDelete(string filePath)
        {
            try
            {
                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                lock (ReversibleActions)
                {
                    if (IsFileAlreadyRecorded(filePath))
                    {
                        return;
                    }

                    string backupPath = Path.Combine(TransactionPath, Guid.NewGuid() + ".bak");
                    File.Copy(filePath, backupPath);

                    var reversibleAction = new ReversibleAction(ActionType.FileDelete, filePath.ToLower())
                    {
                        BackupPath = backupPath,
                        Sequence = ReversibleActions.Count
                    };

                    ReversibleActions.Add(reversibleAction);

                    Utility.EnsureNotNull(transactionLogHandle);

                    this.transactionLogHandle.WriteLine(JsonConvert.SerializeObject(reversibleAction));
                }
                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to record file deletion for for process {this.ProcessId}.", ex);
                throw;
            }
        }

        public void RecordFileAlter(string filePath)
        {
            try
            {
                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                lock (ReversibleActions)
                {
                    if (IsFileAlreadyRecorded(filePath))
                    {
                        return;
                    }

                    string backupPath = Path.Combine(TransactionPath, Guid.NewGuid() + ".bak");
                    File.Copy(filePath, backupPath);

                    var reversibleAction = new ReversibleAction(ActionType.FileAlter, filePath.ToLower())
                    {
                        BackupPath = backupPath,
                        Sequence = ReversibleActions.Count
                    };

                    ReversibleActions.Add(reversibleAction);

                    Utility.EnsureNotNull(transactionLogHandle);

                    this.transactionLogHandle.WriteLine(JsonConvert.SerializeObject(reversibleAction));
                }
                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to record file alteration for for process {this.ProcessId}.", ex);
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
            try
            {
                var ptRollback = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Rollback);
                try
                {
                    var rollbackActions = ReversibleActions.OrderByDescending(o => o.Sequence);

                    foreach (var record in rollbackActions)
                    {
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

                            Utility.EnsureNotNull(diskPath);
                            Utility.EnsureNotNull(record.BackupPath);

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
                            Utility.EnsureNotNull(record.BackupPath);
                            Helpers.CopyDirectory(record.BackupPath, record.OriginalPath);
                        }
                    }

                    transactionManager.RemoveByProcessId(ProcessId);

                    try
                    {
                        CleanupTransaction();
                    }
                    catch
                    {
                        //Discard.
                    }
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
                PT?.AddDescreteMetric(PerformanceTrace.PerformanceTraceDescreteMetricType.TransactionDuration, (DateTime.UtcNow - this.StartTime).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to rollback transaction for for process {this.ProcessId}.", ex);
                throw;
            }
        }

        public void Commit()
        {
            try
            {
                var ptCommit = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Commit);
                lock (this)
                {
                    referenceCount--;

                    if (referenceCount == 0)
                    {
                        try
                        {
                            Utility.EnsureNotNull(DeferredIOs);
                            DeferredIOs.CommitDeferredDiskIO();
                            CleanupTransaction();
                            transactionManager.RemoveByProcessId(ProcessId);
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

                PT?.AddDescreteMetric(PerformanceTrace.PerformanceTraceDescreteMetricType.TransactionDuration, (DateTime.UtcNow - this.StartTime).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to commit transaction for for process {this.ProcessId}.", ex);
                throw;
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

                foreach (var record in ReversibleActions)
                {
                    //Delete all the backup files.
                    if (record.Action == ActionType.FileAlter || record.Action == ActionType.FileDelete)
                    {
                        Utility.EnsureNotNull(record.BackupPath);
                        File.Delete(record.BackupPath);
                    }
                    else if (record.Action == ActionType.DirectoryDelete)
                    {
                        Utility.EnsureNotNull(record.BackupPath);
                        Directory.Delete(record.BackupPath, true);
                    }
                }

                File.Delete(TransactionLogFilePath);
                Directory.Delete(TransactionPath, true);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to cleanup transaction for for process {this.ProcessId}.", ex);
                throw;
            }
        }
    }
}
