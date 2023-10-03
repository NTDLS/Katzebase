using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Library;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectLocks
    {
        private readonly List<ObjectLock> _collection = new();
        private readonly Dictionary<Transaction, LockIntention> _transactionWaitingForLocks = new();
        private readonly Core _core;

        public ObjectLocks(Core core)
        {
            _core = core;
        }

        public void Remove(ObjectLock objectLock)
        {
            try
            {
                lock (CentralCriticalSections.AcquireLock)
                {
                    _collection.Remove(objectLock);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to remove lock.", ex);
                throw;
            }
        }

        public List<ObjectLock> GloballyHeldLocks(string diskPath, LockType lockType)
        {
            lock (CentralCriticalSections.AcquireLock)
            {
                var lockedObjects = new List<ObjectLock>();

                if (lockType == LockType.File)
                {
                    //See if there are any other locks on this file:
                    lockedObjects.AddRange(_collection.Where(o => o.LockType == LockType.File && o.DiskPath == diskPath));
                }

                //See if there are any other locks on the directory containig this file (or directory) or any of its parents.
                //When we lock a directory, we intend all lower directories to be locked too.
                lockedObjects.AddRange(_collection.Where(o => o.LockType == LockType.Directory && diskPath.StartsWith(o.DiskPath)));

                return lockedObjects;
            }
        }

        internal Dictionary<Transaction, LockIntention> CloneTransactionWaitingForLocks()
        {
            lock (_transactionWaitingForLocks)
            {
                return _transactionWaitingForLocks.ToDictionary(o => o.Key, o => o.Value);
            }
        }

        public void Acquire(Transaction transaction, LockIntention intention)
        {
            lock (transaction.GrantedLockCache)
            {
                if (transaction.GrantedLockCache.Contains(intention.Key))
                {
                    return;
                }
            }

            AcquireInternal(transaction, intention);

            lock (transaction.GrantedLockCache)
            {
                transaction.GrantedLockCache.Add(intention.Key);
            }
        }

        private void AcquireInternal(Transaction transaction, LockIntention intention)
        {
            try
            {
                //We keep track of all transactions that are waiting on locks for a few reasons:
                // (1) When we suspect a deadlock we know what all transactions are potentially involved
                // (2) We are safe to poke around those transaction's properties because we know their threda are in this method.
                lock (_transactionWaitingForLocks)
                {
                    _transactionWaitingForLocks.Add(transaction, intention);
                }

                DateTime startTime = DateTime.UtcNow;

                while (true)
                {
                    transaction.EnsureActive();

                    if ((DateTime.UtcNow - startTime).TotalSeconds > _core.Settings.LockWaitTimeoutSeconds)
                    {
                        var lockWaitTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                        _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                        _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);
                        transaction.Rollback();
                        throw new KbTimeoutException($"Timeout exceeded while waiting on lock: {intention.LockType} : {intention.Operation} : '{intention.ObjectName}'");
                    }

                    lock (CentralCriticalSections.AcquireLock)
                    {
                        var lockedObjects = GloballyHeldLocks(intention.DiskPath, intention.LockType); //Find any existing locks:

                        if (lockedObjects.Count == 0)
                        {
                            //No locks on the object exist - so add one to the local and class collections.
                            var lockedObject = new ObjectLock(_core, intention);
                            _collection.Add(lockedObject);
                            lockedObjects.Add(lockedObject);
                        }

                        //We just want read access.
                        if (intention.Operation == LockOperation.Read)
                        {
                            var blockers = lockedObjects.SelectMany(o => o.Keys).Where(o => o.LockOperation == LockOperation.Write && o.ProcessId != transaction.ProcessId).ToList();
                            //If there are no existing un-owned write locks.
                            if (blockers.Count() == 0)
                            {
                                transaction.BlockedBy.Clear();

                                foreach (var lockedObject in lockedObjects)
                                {
                                    lockedObject.Hits++;

                                    if (lockedObject.Keys.Any(o => o.ProcessId == transaction.ProcessId && o.LockOperation == intention.Operation))
                                    {
                                        //Do we really need to hand out multiple keys to the same object of the same type? I dont think we do. Just continue...
                                        continue;
                                    }

                                    var lockKey = lockedObject.IssueSingleUseKey(transaction, intention);
                                    KbUtility.EnsureNotNull(transaction.HeldLockKeys);
                                    transaction.HeldLockKeys.Add(lockKey);
                                }

                                var lockWaitTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                                _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return;
                            }
                            else
                            {
                                transaction.BlockedBy.Clear();
                                transaction.BlockedBy.AddRange(blockers.Select(o => o.ProcessId).Distinct());
                            }
                        }
                        //We want write access.
                        else if (intention.Operation == LockOperation.Write)
                        {
                            var blockers = lockedObjects.SelectMany(o => o.Keys).Where(o => o.ProcessId != transaction.ProcessId).ToList();
                            if (blockers.Count() == 0) //If there are no existing un-owned locks.
                            {
                                transaction.BlockedBy.Clear();

                                foreach (var lockedObject in lockedObjects)
                                {
                                    lockedObject.Hits++;

                                    if (lockedObject.Keys.Any(o => o.ProcessId == transaction.ProcessId && o.LockOperation == intention.Operation))
                                    {
                                        //Do we really need to hand out multiple keys to the same object of the same type? I dont think we do.
                                        continue;
                                    }

                                    var lockKey = lockedObject.IssueSingleUseKey(transaction, intention);
                                    KbUtility.EnsureNotNull(transaction.HeldLockKeys);
                                    transaction.HeldLockKeys.Add(lockKey);
                                }

                                var lockWaitTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                                _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return;
                            }
                            else
                            {
                                transaction.BlockedBy.Clear();
                                transaction.BlockedBy.AddRange(blockers.Select(o => o.ProcessId).Distinct());
                            }
                        }
                    }

                    if (transaction.BlockedBy.Any())
                    {
                        lock (_transactionWaitingForLocks)
                        {
                            //Get a list of all valid transactions.
                            var waitingTransactions = _transactionWaitingForLocks.Keys.Where(o => o.IsDeadlocked == false);

                            //Get a list of transactions that are blocked by the current transaction.
                            var blockedByMe = waitingTransactions.Where(o => o.BlockedBy.Contains(transaction.ProcessId));
                            foreach (var blocked in blockedByMe)
                            {
                                //Check to see if the current transaction is waiting on any of those blocked transaction (circular reference).
                                if (transaction.BlockedBy.Contains(blocked.ProcessId))
                                {
                                    #region Deadlock reporting.

                                    var deadLockId = Guid.NewGuid().ToString();

                                    var explanation = new StringBuilder();

                                    explanation.AppendLine("Deadlock {");
                                    explanation.AppendLine($"    Id: {deadLockId}");
                                    explanation.AppendLine("    Blocking Transactions {");
                                    explanation.AppendLine($"        ProcessId: {transaction.ProcessId}");
                                    explanation.AppendLine($"        ReferenceCount: {transaction.ReferenceCount}");
                                    explanation.AppendLine($"        StartTime: {transaction.StartTime}");

                                    explanation.AppendLine("        Lock Intention {");
                                    explanation.AppendLine($"            ProcessId: {transaction.ProcessId}");
                                    explanation.AppendLine($"            LockType: {intention.LockType}");
                                    explanation.AppendLine($"            Operation: {intention.Operation}");
                                    explanation.AppendLine($"            Object: {intention.DiskPath}");
                                    explanation.AppendLine("        }");

                                    KbUtility.EnsureNotNull(transaction.HeldLockKeys);

                                    explanation.AppendLine("        Held Locks {");
                                    foreach (var key in transaction.HeldLockKeys)
                                    {
                                        explanation.AppendLine($"            ({key.ObjectLock.LockType}) ({key.LockOperation}) {key.ObjectLock.DiskPath}");
                                    }
                                    explanation.AppendLine("        }");

                                    explanation.AppendLine("        Awaiting Locks {");
                                    foreach (var waitingFor in _transactionWaitingForLocks.Where(o => o.Key == transaction))
                                    {
                                        explanation.AppendLine($"            ({waitingFor.Value.LockType}) ({waitingFor.Value.Operation}) {waitingFor.Value.DiskPath}");
                                    }
                                    explanation.AppendLine("        }");

                                    explanation.AppendLine("}");

                                    explanation.AppendLine("Blocked Transaction(s) {");
                                    foreach (var waiter in blockedByMe)
                                    {
                                        explanation.AppendLine($"        ProcessId: {waiter.ProcessId}");
                                        explanation.AppendLine($"        ReferenceCount: {waiter.ReferenceCount}");
                                        explanation.AppendLine($"        StartTime: {waiter.StartTime}");

                                        KbUtility.EnsureNotNull(waiter.HeldLockKeys);

                                        explanation.AppendLine("        Held Locks {");
                                        foreach (var key in waiter.HeldLockKeys)
                                        {
                                            explanation.AppendLine($"            ({key.ObjectLock.LockType}) ({key.LockOperation}) {key.ObjectLock.DiskPath}");
                                        }
                                        explanation.AppendLine("        }");

                                        explanation.AppendLine("        Awaiting Locks {");
                                        foreach (var waitingFor in _transactionWaitingForLocks.Where(o => o.Key == waiter))
                                        {
                                            explanation.AppendLine($"            ({waitingFor.Value.LockType}) ({waitingFor.Value.Operation}) {waitingFor.Value.DiskPath}");
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
                        }
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
                lock (_transactionWaitingForLocks)
                {
                    _transactionWaitingForLocks.Remove(transaction);
                }
            }
        }
    }
}
