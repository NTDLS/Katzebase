using Newtonsoft.Json;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Engine.Locking;
using NTDLS.Katzebase.Engine.Trace;
using NTDLS.Semaphore;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Atomicity
{
    internal class Transaction : IDisposable
    {
        public LockIntention? CurrentLockIntention { get; set; }
        public string TopLevelOperation { get; set; } = string.Empty;
        public Guid Id { get; private set; } = Guid.NewGuid();
        public List<KbQueryResultMessage> Messages { get; private set; } = new();
        public ulong ProcessId { get; private set; }
        public DateTime StartTime { get; private set; }
        public bool IsDeadlocked { get; set; }
        public PerformanceTrace? PT { get; private set; } = null;
        public CriticalSection CriticalSectionTransaction { get; } = new();

        /// <summary>
        /// Whether the transaction was user created or not. The server implicitly creates lightweight transactions for everyhting.
        /// </summary>
        public bool IsUserCreated { get; set; }

        private readonly EngineCore _core;
        private TransactionManager? _transactionManager;
        private StreamWriter? _transactionLogHandle = null;
        private readonly CriticalResource<Dictionary<KbTransactionWarning, HashSet<string>>> _warnings = new();

        public bool IsComittedOrRolledBack { get; private set; } = false;
        public bool IsCancelled { get; private set; } = false;

        private int _referenceCount = 0;
        public int ReferenceCount
        {
            set => CriticalSectionTransaction.Use(() => _referenceCount = value);
            get => CriticalSectionTransaction.Use(() => _referenceCount);
        }

        #region Critical objects (Any object in this region must be locked for access).

        /// <summary>
        /// Write-cached objects that need to be flushed to disk upon commit.
        /// </summary>
        public CriticalResource<DeferredDiskIO> DeferredIOs { get; private set; } = new();
        /// <summary>
        /// Files that have been read by the transaction. These will be placed into read
        /// cache and since they can be modified in memory, the cached items must be removed upon rollback.
        /// </summary>
        public CriticalResource<HashSet<string>> FilesReadForCache { get; set; } = new();

        /// <summary>
        /// All abortable operations that the transaction has performed.
        /// </summary>
        public CriticalResource<List<Atom>> Atoms { get; private set; } = new();

        /// <summary>
        /// We keep a hashset of locks granted to this transaction by the LockIntention.Key so that we
        ///     do not have to perform blocking or deadlock checks again for the life of this transaction.
        /// </summary>
        public CriticalResource<HashSet<string>> GrantedLockCache { get; private set; }

        /// <summary>
        /// Outstanding lock-keys that are blocking this transaction.
        /// </summary>
        public CriticalResource<List<ObjectLockKey>> BlockedByKeys { get; private set; }

        /// <summary>
        /// Lock if you need to read/write.
        /// All lock-keys that are currently held by the transaction.
        /// </summary>
        public CriticalResource<List<ObjectLockKey>> HeldLockKeys { get; private set; }

        /// <summary>
        /// Lock if you need to read/write.
        /// Any temporary schemas that have been created in this transaction.
        /// </summary>
        public CriticalResource<HashSet<string>> TemporarySchemas { get; private set; } = new();

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
                IsComittedOrRolledBack = IsComittedOrRolledBack,
                IsCancelled = IsCancelled
            };

            GrantedLockCache.Use((obj) => { snapshot.GrantedLockCache = new HashSet<string>(obj); });
            BlockedByKeys.Use((obj) => { snapshot.BlockedByKeys = obj.Select(o => o.Snapshot()).ToList(); });
            HeldLockKeys.Use((obj) => { snapshot.HeldLockKeys = obj.Select(o => o.Snapshot()).ToList(); });
            TemporarySchemas.Use((obj) => { snapshot.TemporarySchemas = new HashSet<string>(obj); });
            FilesReadForCache.Use((obj) => { snapshot.FilesReadForCache = new HashSet<string>(obj); });
            DeferredIOs.Use((obj) => { snapshot.DeferredIOs = obj.Snapshot(); });
            Atoms.Use((obj) => { snapshot.Atoms = obj.Select(o => o.Snapshot()).ToList(); });

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
        {
            Messages.Add(new KbQueryResultMessage(text, type));
        }

        private void ReleaseLocks()
        {
            GrantedLockCache.Use((obj) => obj.Clear());

            HeldLockKeys.Use((obj) =>
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
                EnsureActive();

                var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"File:{lockOperation}");

                diskpath = diskpath.ToLower();

                var lockIntention = new LockIntention(diskpath, LockGranularity.File, lockOperation);
                _core.Locking.Locks.Acquire(this, lockIntention);
                ptLock?.StopAndAccumulate();
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
                EnsureActive();

                var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"Directory:{lockOperation}");

                diskpath = diskpath.ToLower();

                var lockIntention = new LockIntention(diskpath, LockGranularity.Directory, lockOperation);
                _core.Locking.Locks.Acquire(this, lockIntention);

                ptLock?.StopAndAccumulate();
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
                EnsureActive();

                var ptLock = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Lock, $"Directory:{lockOperation}");

                diskpath = diskpath.ToLower();

                var lockIntention = new LockIntention(diskpath, LockGranularity.Path, lockOperation);
                _core.Locking.Locks.Acquire(this, lockIntention);

                ptLock?.StopAndAccumulate();
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

        public Transaction(EngineCore core, TransactionManager transactionManager, ulong processId, bool isRecovery)
        {
            _core = core;
            _transactionManager = transactionManager;

            GrantedLockCache = new(core.Locking.CriticalSectionLockManagement);
            BlockedByKeys = new(core.Locking.CriticalSectionLockManagement);
            HeldLockKeys = new(core.Locking.CriticalSectionLockManagement);

            StartTime = DateTime.UtcNow;
            ProcessId = processId;

            DeferredIOs.Use((obj) =>
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

                KbUtility.EnsureNotNull(_transactionLogHandle);
            }
        }

        #region Action Recorders.

        private bool IsFileAlreadyRecorded(string filePath)
        {
            bool result = false;

            Atoms.Use((obj) =>
            {
                result = obj.Exists(o => o.Key == filePath.ToLower());
            });

            return result;
        }

        public void RecordFileCreate(string filePath)
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                EnsureActive();

                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                Atoms.Use((obj) =>
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

                    KbUtility.EnsureNotNull(_transactionLogHandle);

                    _transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
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
            KbUtility.EnsureNotNull(_core);

            try
            {
                EnsureActive();

                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);
                Atoms.Use((obj) =>
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

                    KbUtility.EnsureNotNull(_transactionLogHandle);

                    _transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
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
            KbUtility.EnsureNotNull(_core);

            try
            {
                EnsureActive();

                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                DeferredIOs.Use((obj) => obj.RemoveItemsWithPrefix(diskPath));

                Atoms.Use((obj) =>
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
                        Sequence = obj.Count
                    };

                    obj.Add(atom);

                    KbUtility.EnsureNotNull(_transactionLogHandle);

                    _transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
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
            KbUtility.EnsureNotNull(_core);

            try
            {
                EnsureActive();

                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                DeferredIOs.Use((obj) => obj.Remove(filePath));

                Atoms.Use((obj) =>
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

                    KbUtility.EnsureNotNull(_transactionLogHandle);

                    _transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
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
            KbUtility.EnsureNotNull(_core);

            try
            {
                EnsureActive();

                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                FilesReadForCache.UseNullable((obj) => obj.Add(filePath));

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
            KbUtility.EnsureNotNull(_core);

            try
            {
                EnsureActive();

                var ptRecording = PT?.CreateDurationTracker(PerformanceTrace.PerformanceTraceCumulativeMetricType.Recording);

                Atoms.Use((obj) =>
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

                    KbUtility.EnsureNotNull(_transactionLogHandle);

                    _transactionLogHandle.WriteLine(JsonConvert.SerializeObject(atom));
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
        {
            CriticalSectionTransaction.Use(() => _referenceCount++);
        }

        public void Rollback()
        {
            KbUtility.EnsureNotNull(_core);

            CriticalSectionTransaction.Use(() =>
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
                        Atoms.Use((obj) =>
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

                            FilesReadForCache.Use((obj) =>
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
                    PT?.AddDescreteMetric(PerformanceTrace.PerformanceTraceDescreteMetricType.TransactionDuration, (DateTime.UtcNow - StartTime).TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    _core.Log.Write($"Failed to rollback transaction for process {ProcessId}.", ex);
                    throw;
                }
            });
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

            return CriticalSectionTransaction.Use(() =>
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
                    _referenceCount--;

                    if (_referenceCount == 0)
                    {
                        IsComittedOrRolledBack = true;

                        try
                        {
                            DeferredIOs.Use((obj) => obj.CommitDeferredDiskIO());
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
            KbUtility.EnsureNotNull(_core);

            TemporarySchemas.Use((obj) =>
            {
                if (obj.Any())
                {
                    using (var ephemeralTxRef = _core.Transactions.Acquire(ProcessId))
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
            KbUtility.EnsureNotNull(_core);

            try
            {
                if (_transactionLogHandle != null)
                {
                    _transactionLogHandle.Close();
                    _transactionLogHandle.Dispose();
                    _transactionLogHandle = null;
                }

                Atoms.Use((obj) =>
                {
                    foreach (var record in obj)
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
