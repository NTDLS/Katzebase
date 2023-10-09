using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Semaphore;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectLocks
    {
        private readonly CriticalResource<List<ObjectLock>> _collection = new();
        private readonly CriticalResource<Dictionary<Transaction, LockIntention>> _transactionWaitingForLocks = new();
        private EngineCore? _core;

        public void SetCore(EngineCore core)
        {
            _core = core;
        }

        public void Remove(ObjectLock objectLock)
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                _collection.Use((obj) => obj.Remove(objectLock));
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to remove lock.", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns a set (if any) of existing locks that that would conflict with the given lock intention.
        /// </summary>
        /// <param name="intention"></param>
        /// <returns></returns>
        public HashSet<ObjectLock> GetConflictingLocks(LockIntention intention)
        {
            var result = _collection.UseNullable((obj) =>
            {
                var lockedObjects = new HashSet<ObjectLock>();

                var intentionDirectory = Path.GetDirectoryName(intention.DiskPath) ?? string.Empty;

                //If we are locking a file, then look for all other locks for the exact path.
                if (intention.Granularity == LockGranularity.File)
                {
                    var fileLocks = obj.Where(o =>
                        o.Granularity == LockGranularity.File && o.DiskPath == intention.DiskPath);
                    foreach (var existingLock in fileLocks)
                    {
                        lockedObjects.Add(existingLock);
                    }
                }

                //Check if the intended file or directory is in a locked directory.
                var exactDirectoryLocks = obj.Where(o =>
                    (o.Granularity == LockGranularity.Directory || o.Granularity == LockGranularity.Path) && intention.DiskPath == intentionDirectory);
                foreach (var existingLock in exactDirectoryLocks)
                {
                    lockedObjects.Add(existingLock);
                }

                var direcotryAndSubPathLocks = obj.Where(o =>
                    o.Granularity == LockGranularity.Path && intentionDirectory.StartsWith(o.DiskPath));
                foreach (var existingLock in direcotryAndSubPathLocks)
                {
                    lockedObjects.Add(existingLock);
                }

                /* TODO: Think though this, seems agressive. If we need to lock a parent path then we should just do it explicitly.
                //If the intended lock is a path then we need to find all existing locks that would be contained in the intended lock path.
                //This is done by looking for all existing locks where the existing lock path starts with the intended lock path.
                if (intention.Granularity == LockGranularity.Path)
                {
                    var higherLevelDirectoryLocks = _collection.Where(o => o.DiskPath.StartsWith(intentionDirectory));
                    foreach (var existingLock in higherLevelDirectoryLocks)
                    {
                        lockedObjects.Add(existingLock);
                    }
                }
                */

                return lockedObjects;
            });

            KbUtility.EnsureNotNull(result);

            return result;
        }

        internal Dictionary<TransactionSnapshot, LockIntention> SnapshotWaitingTransactions()
        {
            return _transactionWaitingForLocks.Use((obj) => obj.ToDictionary(o => o.Key.Snapshot(), o => o.Value));
        }

        public void Acquire(Transaction transaction, LockIntention intention)
        {
            if (transaction.GrantedLockCache.UseNullable((obj) =>
            {
                return obj.Contains(intention.Key);
            }))
            {
                return;
            }

            AcquireInternal(transaction, intention);

            transaction.GrantedLockCache.UseNullable((obj) => obj.Add(intention.Key));
        }

        private void AcquireInternal(Transaction transaction, LockIntention intention)
        {
            KbUtility.EnsureNotNull(_core);

            try
            {
                //We keep track of all transactions that are waiting on locks for a few reasons:
                // (1) When we suspect a deadlock we know what all transactions are potentially involved
                // (2) We are safe to poke around those transaction's properties because we know their threda are in this method.
                _transactionWaitingForLocks.Use((obj) => obj.Add(transaction, intention));

                while (true)
                {
                    transaction.EnsureActive();

                    transaction.CurrentLockIntention = intention;

                    if (_core.Settings.LockWaitTimeoutSeconds > 0 && (DateTime.UtcNow - intention.CreationTime).TotalSeconds > _core.Settings.LockWaitTimeoutSeconds)
                    {
                        var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                        _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                        _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);
                        transaction.Rollback();
                        throw new KbTimeoutException($"Timeout exceeded while waiting on lock: {intention.ToString()}");
                    }

                    bool? transactionAcquiredLock = _collection.TryUseAll<bool>(new ICriticalResource[] {
                        transaction.TransactionGranularitySync, transaction.HeldLockKeys, transaction.BlockedByKeys }, out bool isLockHeld, (obj) =>
                    {
                        var lockedObjects = GetConflictingLocks(intention); //Find any existing locks on the given lock intention.

                        if (lockedObjects.Count == 0)
                        {
                            //No locks on the object exist - so add one to the local and class collections.
                            var lockedObject = new ObjectLock(_core, intention);
                            obj.Add(lockedObject);
                            lockedObjects.Add(lockedObject);
                        }

                        //We just want read access.
                        if (intention.Operation == LockOperation.Read)
                        {
                            var blockers = lockedObjects.SelectMany(o => o.Keys.Use((obj) => obj)).Where(o => o.Operation == LockOperation.Write && o.ProcessId != transaction.ProcessId).ToList();
                            //If there are no existing un-owned write locks.
                            if (blockers.Any() == false)
                            {
                                transaction.BlockedByKeys.Use((obj) => obj.Clear());

                                foreach (var lockedObject in lockedObjects)
                                {
                                    lockedObject.Hits++;

                                    if (lockedObject.Keys.Use((obj) => obj).Any(o => o.ProcessId == transaction.ProcessId && o.Operation == intention.Operation))
                                    {
                                        //Do we really need to hand out multiple keys to the same object of the same type? I dont think we do. Just continue...
                                        continue;
                                    }

                                    var lockKey = lockedObject.IssueSingleUseKey(transaction, intention);
                                    transaction.HeldLockKeys.Use((obj) => obj.Add(lockKey));
                                }

                                var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                                _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return true;
                            }
                            else
                            {
                                transaction.BlockedByKeys.Use((obj) => obj.Clear());
                                transaction.BlockedByKeys.Use((obj) => obj.AddRange(blockers.Distinct()));
                            }
                        }
                        //We want write access.
                        else if (intention.Operation == LockOperation.Write)
                        {
                            var blockers = lockedObjects.SelectMany(o => o.Keys.Use((obj) => obj)).Where(o => o.ProcessId != transaction.ProcessId).ToList();
                            if (blockers.Any() == false) //If there are no existing un-owned locks.
                            {
                                transaction.BlockedByKeys.Use((obj) => obj.Clear());

                                foreach (var lockedObject in lockedObjects)
                                {
                                    lockedObject.Hits++;

                                    if (lockedObject.Keys.Use((obj) => obj.Any(o => o.ProcessId == transaction.ProcessId && o.Operation == intention.Operation)))
                                    {
                                        //Do we really need to hand out multiple keys to the same object of the same type? I dont think we do.
                                        continue;
                                    }

                                    var lockKey = lockedObject.IssueSingleUseKey(transaction, intention);

                                    transaction.HeldLockKeys.Use((obj) => obj.Add(lockKey));
                                }

                                var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                                _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return true;
                            }
                            else
                            {
                                transaction.BlockedByKeys.Use((obj) => obj.Clear());
                                transaction.BlockedByKeys.Use((obj) => obj.AddRange(blockers.Distinct()));
                            }
                        }

                        transaction.BlockedByKeys.Use((obj) =>
                        {
                            if (obj.Any())
                            {
                                _transactionWaitingForLocks.Use((txWaitingForLocks) =>
                                {
                                    //Get a list of all valid transactions.
                                    var waitingTransactions = txWaitingForLocks.Keys.Where(o => o.IsDeadlocked == false);

                                    //Get a list of transactions that are blocked by the current transaction.
                                    var blockedByMe = waitingTransactions.Where(
                                        o => o.BlockedByKeys.UseNullable((obj) => obj.Where(k => k.ProcessId == transaction.ProcessId).Any())).ToList();

                                    foreach (var blocked in blockedByMe)
                                    {
                                        //Check to see if the current transaction is waiting on any of those blocked transaction (circular reference).
                                        if (obj.Where(o => o.ProcessId == blocked.ProcessId).Any())
                                        {
                                            #region Deadlock reporting.

                                            var deadLockId = Guid.NewGuid().ToString();

                                            var explanation = new StringBuilder();

                                            explanation.AppendLine("Deadlock {");
                                            explanation.AppendLine($"    Id: {deadLockId}");
                                            explanation.AppendLine("    Blocking Transactions {");
                                            explanation.AppendLine($"        ProcessId: {transaction.ProcessId}");
                                            explanation.AppendLine($"        Operation: {transaction.TopLevelOperation}");
                                            explanation.AppendLine($"        ReferenceCount: {transaction.ReferenceCount}");
                                            explanation.AppendLine($"        StartTime: {transaction.StartTime}");

                                            explanation.AppendLine("        Lock Intention {");
                                            explanation.AppendLine($"            ProcessId: {transaction.ProcessId}");
                                            explanation.AppendLine($"            Granularity: {intention.Granularity}");
                                            explanation.AppendLine($"            Operation: {intention.Operation}");
                                            explanation.AppendLine($"            Object: {intention.DiskPath}");
                                            explanation.AppendLine("        }");

                                            KbUtility.EnsureNotNull(transaction.HeldLockKeys);

                                            explanation.AppendLine("        Held Locks {");
                                            transaction.HeldLockKeys.Use((obj) =>
                                            {
                                                foreach (var key in obj)
                                                {
                                                    explanation.AppendLine($"            {key.ToString()}");
                                                }
                                            });
                                            explanation.AppendLine("        }");

                                            explanation.AppendLine("        Awaiting Locks {");
                                            foreach (var waitingFor in txWaitingForLocks.Where(o => o.Key == transaction))
                                            {
                                                explanation.AppendLine($"            {waitingFor.Value.ToString()}");
                                            }
                                            explanation.AppendLine("        }");

                                            explanation.AppendLine("}");

                                            explanation.AppendLine("Blocked Transaction(s) {");
                                            foreach (var waiter in blockedByMe)
                                            {
                                                explanation.AppendLine($"        ProcessId: {waiter.ProcessId}");
                                                explanation.AppendLine($"        Operation: {waiter.TopLevelOperation}");
                                                explanation.AppendLine($"        ReferenceCount: {waiter.ReferenceCount}");
                                                explanation.AppendLine($"        StartTime: {waiter.StartTime}");

                                                KbUtility.EnsureNotNull(waiter.HeldLockKeys);

                                                explanation.AppendLine("        Held Locks {");

                                                waiter.HeldLockKeys.Use((obj) =>
                                                {
                                                    foreach (var key in obj)
                                                    {
                                                        explanation.AppendLine($"            {key.ToString()}");
                                                    }
                                                });
                                                explanation.AppendLine("        }");

                                                explanation.AppendLine("        Awaiting Locks {");
                                                foreach (var waitingFor in txWaitingForLocks.Where(o => o.Key == waiter))
                                                {
                                                    explanation.AppendLine($"            {waitingFor.Value.ToString()}");
                                                }
                                                explanation.AppendLine("        }");
                                            }
                                            explanation.AppendLine("    }");
                                            explanation.AppendLine("}");

                                            transaction.AddMessage(explanation.ToString(), KbConstants.KbMessageType.Deadlock);

                                            #endregion

                                            transaction.IsDeadlocked = true;
                                            transaction.Rollback();

                                            _core.Health.Increment(HealthCounterType.DeadlockCount);

                                            throw new KbDeadlockException($"Deadlock occurred, transaction for process {transaction.ProcessId} is being terminated.", explanation.ToString());
                                        }
                                    }
                                });
                            }
                        });
                        return false;
                    });

                    if (transactionAcquiredLock == true)
                    {
                        return;
                    }

                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to acquire lock for process {transaction.ProcessId}.", ex);
                throw;
            }
            finally
            {
                transaction.CurrentLockIntention = null;
                _transactionWaitingForLocks.Use((obj) => obj.Remove(transaction));
            }
        }
    }
}
