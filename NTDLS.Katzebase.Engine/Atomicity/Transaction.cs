using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.Engine.Locking;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Engine.Trace;
using NTDLS.Semaphore;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Atomicity
{
    internal class Transaction : IDisposable
    {
        public ObjectLockIntention? CurrentLockIntention { get; set; }
        public string TopLevelOperation { get; set; } = string.Empty;
        public Guid Id { get; private set; } = Guid.NewGuid();
        public List<KbQueryResultMessage> Messages { get; private set; } = new();
        public ulong ProcessId { get; private set; }
        public SessionState Session => _core.Sessions.ByProcessId(ProcessId);

        public DateTime StartTime { get; private set; }
        public bool IsDeadlocked { get; private set; }
        public PerformanceTrace? PT { get; private set; } = null;
        public OptimisticSemaphore CriticalSectionTransaction { get; } = new();

        /// <summary>
        /// Whether the transaction was user created or not. The server implicitly creates lightweight transactions for everything.
        /// </summary>
        public bool IsUserCreated { get; set; }

        private readonly EngineCore _core;
        private TransactionManager? _transactionManager;
        private StreamWriter? _transactionLogHandle = null;
        private readonly PessimisticCriticalResource<Dictionary<KbTransactionWarning, HashSet<string>>> _warnings = new();

        public bool IsCommittedOrRolledBack { get; private set; } = false;
        public bool IsCancelled { get; private set; } = false;

        private int _referenceCount = 0;
        public int ReferenceCount
        {
            set => CriticalSectionTransaction.Write(() => _referenceCount = value);
            get => CriticalSectionTransaction.Read(() => _referenceCount);
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
        public OptimisticCriticalResource<HashSet<string>> FilesReadForCache { get; set; } = new();

        /// <summary>
        /// All abortable operations that the transaction has performed.
        /// </summary>
        public OptimisticCriticalResource<List<Atom>> Atoms { get; private set; } = new();

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
                IsCommittedOrRolledBack = IsCommittedOrRolledBack,
                IsCancelled = IsCancelled
            };

            GrantedLockCache.Read((obj) => { snapshot.GrantedLockCache = new HashSet<string>(obj); });
            BlockedByKeys.Read((obj) => { snapshot.BlockedByKeys = obj.Select(o => o.Snapshot()).ToList(); });
            HeldLockKeys.Read((obj) => { snapshot.HeldLockKeys = obj.Select(o => o.Snapshot()).ToList(); });
            TemporarySchemas.Read((obj) => { snapshot.TemporarySchemas = new HashSet<string>(obj); });
            FilesReadForCache.Read((obj) => { snapshot.FilesReadForCache = new HashSet<string>(obj); });
            DeferredIOs.Read((obj) => { snapshot.DeferredIOs = obj.Snapshot(); });
            Atoms.Read((obj) => { snapshot.Atoms = obj.Select(o => o.Snapshot()).ToList(); });

            return snapshot;
        }

        public void AddWarning(KbTransactionWarning warning, string message = "")
        {
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
                    //No need to duplicate or blank any messages.
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
            _core.Health.Increment(HealthCounterType.DeadlockCount);
        }

        private void ReleaseLocks()
        {
            GrantedLockCache.Write((obj) => obj.Clear());

            HeldLockKeys.Write((obj) =>
            {
                foreach (var key in obj)
                {
                    key.TurnInKey();
                }
            });
        }

        public void EnsureActive()
        {
            if (IsCancelled)
            {
                throw new KbTransactionCancelledException("The transaction was cancelled");
            }
            else if (IsDeadlocked)
            {
                throw new KbTransactionCancelledException("The transaction was deadlocked");
            }
            else if (IsCommittedOrRolledBack)
            {
                throw new KbTransactionCancelledException("The transaction was committed or rolled back.");
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

        public void ConvertLockToStability(ObjectLockKey? lockKey)
        {
            if (lockKey != null)
            {
                _core.Locking.Locks.ConvertToStability(this, lockKey);
            }
        }

        public ObjectLockKey? LockFile(LockOperation lockOperation, string diskPath)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"File:{lockOperation}");

                diskPath = diskPath.ToLowerInvariant();

                var lockIntention = new ObjectLockIntention(diskPath, LockGranularity.File, lockOperation);
                var result = _core.Locking.Locks.Acquire(this, lockIntention);
                ptLock?.StopAndAccumulate();

                return result;
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to acquire file lock.", ex);
                throw;
            }
        }

        /// <summary>
        /// Locks a single directory and all files (but not sub-directories) that it contains.
        /// </summary>
        /// <param name="lockOperation"></param>
        /// <param name="diskPath"></param>
        public ObjectLockKey? LockDirectory(LockOperation lockOperation, string diskPath)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"Directory:{lockOperation}");

                diskPath = diskPath.ToLowerInvariant();

                var lockIntention = new ObjectLockIntention(diskPath, LockGranularity.Directory, lockOperation);
                var result = _core.Locking.Locks.Acquire(this, lockIntention);
                ptLock?.StopAndAccumulate();

                return result;
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to acquire file lock.", ex);
                throw;
            }
        }

        /// <summary>
        /// Locks a path (which means the directory, sub-directory and all files beneath it).
        /// </summary>
        /// <param name="lockOperation"></param>
        /// <param name="diskPath"></param>
        public ObjectLockKey? LockPath(LockOperation lockOperation, string diskPath)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"Directory:{lockOperation}");

                diskPath = diskPath.ToLowerInvariant();

                var lockIntention = new ObjectLockIntention(diskPath, LockGranularity.RecursiveDirectory, lockOperation);
                var result = _core.Locking.Locks.Acquire(this, lockIntention);
                ptLock?.StopAndAccumulate();

                return result;
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

            GrantedLockCache = new(core.CriticalSectionLockManagement);
            BlockedByKeys = new(core.CriticalSectionLockManagement);
            HeldLockKeys = new(core.CriticalSectionLockManagement);

            StartTime = DateTime.UtcNow;
            ProcessId = processId;

            DeferredIOs.Write((obj) =>
            {
                obj.SetCore(core);
            });

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

                _transactionLogHandle.EnsureNotNull();
            }
        }

        #region Action Recorders.

        private bool IsFileAlreadyRecorded(string filePath)
        {
            bool result = false;

            Atoms.Read((obj) =>
            {
                result = obj.Exists(o => o.Key.Equals(filePath, StringComparison.InvariantCultureIgnoreCase));
            });

            return result;
        }

        public void RecordFileCreate(string filePath)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                Atoms.Write((obj) =>
                {
                    if (IsFileAlreadyRecorded(filePath))
                    {
                        return;
                    }

                    var atom = new Atom(ActionType.FileCreate, filePath)
                    {
                        Sequence = obj.Count
                    };

                    obj.Add(atom);

                    _transactionLogHandle.EnsureNotNull().WriteLine(JsonConvert.SerializeObject(atom));
                });

                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to record file creation for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordDirectoryCreate(string path)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);
                Atoms.Write((obj) =>
                {
                    if (IsFileAlreadyRecorded(path))
                    {
                        return;
                    }

                    var atom = new Atom(ActionType.DirectoryCreate, path)
                    {
                        Sequence = obj.Count
                    };

                    obj.Add(atom);

                    _transactionLogHandle.EnsureNotNull().WriteLine(JsonConvert.SerializeObject(atom));
                });
                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to record file creation for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordPathDelete(string diskPath)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                DeferredIOs.Write((obj) => obj.RemoveItemsWithPrefix(diskPath));

                Atoms.Write((obj) =>
                {
                    if (IsFileAlreadyRecorded(diskPath))
                    {
                        return;
                    }

                    string backupPath = Path.Combine(TransactionPath, Guid.NewGuid().ToString());
                    Directory.CreateDirectory(backupPath);
                    Library.Helpers.CopyDirectory(diskPath, backupPath);

                    var atom = new Atom(ActionType.DirectoryDelete, diskPath)
                    {
                        BackupPath = backupPath,
                        Sequence = obj.Count
                    };

                    obj.Add(atom);

                    _transactionLogHandle.EnsureNotNull().WriteLine(JsonConvert.SerializeObject(atom));
                });
                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to record file deletion for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordFileDelete(string filePath)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                DeferredIOs.Write((obj) => obj.Remove(filePath));

                Atoms.Write((obj) =>
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
                        Sequence = obj.Count
                    };

                    obj.Add(atom);

                    _transactionLogHandle.EnsureNotNull().WriteLine(JsonConvert.SerializeObject(atom));
                });
                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to record file deletion for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordFileRead(string filePath)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                FilesReadForCache.WriteNullable((obj) => obj.Add(filePath));

                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to record file read for process {ProcessId}.", ex);
                throw;
            }
        }

        public void RecordFileAlter(string filePath)
        {
            _core.EnsureNotNull();

            try
            {
                EnsureActive();

                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                Atoms.Write((obj) =>
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
                        Sequence = obj.Count
                    };

                    obj.Add(atom);

                    _transactionLogHandle.EnsureNotNull().WriteLine(JsonConvert.SerializeObject(atom));
                });
                ptRecording?.StopAndAccumulate();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to record file alteration for process {ProcessId}.", ex);
                throw;
            }
        }

        #endregion

        public void AddReference()
           => CriticalSectionTransaction.Write(() => _referenceCount++);

        public void Rollback()
        {
            _core.EnsureNotNull();

            CriticalSectionTransaction.Write(() =>
            {
                if (IsCommittedOrRolledBack)
                {
                    return;
                }

                IsCommittedOrRolledBack = true;
                IsCancelled = true;

                try
                {
                    var ptRollback = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Rollback);
                    try
                    {
                        Atoms.Write((obj) =>
                        {
                            var rollbackActions = obj.OrderByDescending(o => o.Sequence);

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
                                    Library.Helpers.RemoveDirectoryIfEmpty(Path.GetDirectoryName(record.OriginalPath));
                                }
                                else if (record.Action == ActionType.FileAlter || record.Action == ActionType.FileDelete)
                                {
                                    var diskPath = Path.GetDirectoryName(record.OriginalPath);

                                    Directory.CreateDirectory(diskPath.EnsureNotNull());
                                    File.Copy(record.BackupPath.EnsureNotNull(), record.OriginalPath, true);
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
                                    Library.Helpers.CopyDirectory(record.BackupPath.EnsureNotNull(), record.OriginalPath);
                                }
                            }

                            FilesReadForCache.Write((obj) =>
                            {
                                foreach (var file in obj)
                                {
                                    //Un-cache files that we have read too. These might just be persistent in cache and never written and can affect state.
                                    _core.Cache.Remove(file);
                                }
                            });

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
                        });
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
                    PT?.AddDiscreteMetric(PerformanceTrace.PerformanceTraceDiscreteMetricType.TransactionDuration, (DateTime.UtcNow - StartTime).TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    _core.Log.Write($"Failed to rollback transaction for process {ProcessId}.", ex);
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

            return CriticalSectionTransaction.Write(() =>
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
                    var ptCommit = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Commit);
                    _referenceCount--;

                    if (_referenceCount == 0)
                    {
                        IsCommittedOrRolledBack = true;

                        try
                        {
                            DeferredIOs.Write((obj) => obj.CommitDeferredDiskIO());
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
                        PT?.AddDiscreteMetric(PerformanceTrace.PerformanceTraceDiscreteMetricType.TransactionDuration, (DateTime.UtcNow - StartTime).TotalMilliseconds);
                        return true;
                    }
                    else if (_referenceCount < 0)
                    {
                        throw new KbGenericException("Transaction reference count fell below zero.");
                    }
                }
                catch (Exception ex)
                {
                    _core.Log.Write($"Failed to commit transaction for process {ProcessId}.", ex);
                    throw;
                }
                return false;
            });
        }

        private void DeleteTemporarySchemas()
        {
            _core.EnsureNotNull();

            TemporarySchemas.Write((obj) =>
            {
                if (obj.Any())
                {
                    using (var ephemeralTxRef = _core.Transactions.Acquire(Session))
                    {
                        foreach (var tempSchema in obj)
                        {
                            _core.Schemas.Drop(ephemeralTxRef.Transaction, tempSchema);
                        }
                        ephemeralTxRef.Commit();
                    }
                }
            });
        }

        private void CleanupTransaction()
        {
            _core.EnsureNotNull();

            try
            {
                if (_transactionLogHandle != null)
                {
                    _transactionLogHandle.Close();
                    _transactionLogHandle.Dispose();
                    _transactionLogHandle = null;
                }

                Atoms.Write((obj) =>
                {
                    foreach (var record in obj)
                    {
                        //Delete all the backup files.
                        if (record.Action == ActionType.FileAlter || record.Action == ActionType.FileDelete)
                        {
                            File.Delete(record.BackupPath.EnsureNotNull());
                        }
                        else if (record.Action == ActionType.DirectoryDelete)
                        {
                            Directory.Delete(record.BackupPath.EnsureNotNull(), true);
                        }
                    }
                });

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
