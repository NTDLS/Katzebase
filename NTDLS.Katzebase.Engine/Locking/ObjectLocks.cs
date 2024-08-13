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
        private readonly OptimisticCriticalResource<List<ObjectLock>> _collection;
        private readonly OptimisticCriticalResource<Dictionary<Transaction, ObjectLockIntention>> _transactionWaitingForLocks;
        private readonly EngineCore _core;

        public ObjectLocks(EngineCore core)
        {
            _core = core;
            _collection = new(core.LockManagementSemaphore);
            _transactionWaitingForLocks = new(core.LockManagementSemaphore);
        }

        public void Release(ObjectLock objectLock)
        {
            try
            {
                _collection.Write((obj) => obj.Remove(objectLock));
            }
            catch (Exception ex)
            {
                Interactions.Management.LogManager.Error("Failed to remove lock.", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns a set (if any) of existing locks that that would conflict with the given lock intention.
        /// </summary>
        /// <param name="intention"></param>
        /// <returns></returns>
        public HashSet<ObjectLock> GetOverlappingLocks(ObjectLockIntention intention)
        {
            var result = _collection.Read((existingLocks) =>
            {
                var result = new HashSet<ObjectLock>();

                var intentionDirectory = Path.GetDirectoryName(intention.DiskPath) ?? string.Empty;

                //If we are locking a file, then look for all other locks for the exact path.
                if (intention.Granularity == LockGranularity.File)
                {
                    var fileLocks = existingLocks.Where(o =>
                        o.Granularity == LockGranularity.File
                        && o.DiskPath == intention.DiskPath).ToList();

                    fileLocks.ForEach(o => result.Add(o));
                }

                //Check if the intended file or directory is in a locked directory.
                var exactDirectoryLocks = existingLocks.Where(o =>
                    (o.Granularity == LockGranularity.Directory)
                    && o.DiskPath == intentionDirectory).ToList();

                exactDirectoryLocks.ForEach(o => result.Add(o));

                var directoryAndSubPathLocks = existingLocks.Where(o =>
                    o.Granularity == LockGranularity.RecursiveDirectory
                    && intentionDirectory.StartsWith(o.DiskPath)).ToList();

                directoryAndSubPathLocks.ForEach(o => result.Add(o));

                return result;
            });

            return result;
        }

        internal Dictionary<TransactionSnapshot, ObjectLockIntention> SnapshotWaitingTransactions()
            => _transactionWaitingForLocks.Read((obj) => obj.ToDictionary(o => o.Key.Snapshot(), o => o.Value));

        public ObjectLockKey? Acquire(Transaction transaction, ObjectLockIntention intention)
        {
            if (transaction.GrantedLockCache.Read((obj) => obj.Contains(intention.Key)))
            {
                //This transaction owns the lock, but it was created with a previous call to Acquire().
                //  This means that the transaction has the key, but the caller will not be be provided
                //  with it since modification by a non-creator caller would be dangerous.
                //
                //Additionally, we will not issue a new SingleUseKey for the lock because that would be wasteful.
                return null;
            }

            var lockKey = AcquireInternal(transaction, intention);

            transaction.GrantedLockCache.Write((obj) => obj.Add(intention.Key));

            return lockKey;
        }

        private ObjectLockKey AcquireInternal(Transaction transaction, ObjectLockIntention intention)
        {
            try
            {
                //We keep track of all transactions that are waiting on locks for a few reasons:
                // (1) When we suspect a deadlock we know what all transactions are potentially involved.
                // (2) We are safe to poke around those transaction's properties because we know their threads are working in this function.
                _transactionWaitingForLocks.Write((obj) => obj.Add(transaction, intention));

                while (true)
                {
                    transaction.EnsureActive();

                    transaction.CurrentLockIntention = intention;

                    if (_core.Settings.LockWaitTimeoutSeconds > 0
                        && (DateTime.UtcNow - intention.CreationTime).TotalSeconds > _core.Settings.LockWaitTimeoutSeconds)
                    {
                        var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                        _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                        _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);
                        transaction.Rollback();
                        throw new KbTimeoutException($"Timeout exceeded while waiting on lock: {intention.ToString()}");
                    }

                    //Since _collection, tx.GrantedLockCache, tx.HeldLockKeys and tx.BlockedByKeys all use the critical
                    //  section "Locking.CriticalSectionLockManagement", we will only need:
                    var acquiredLockKey = _collection.TryWriteAll([transaction.TransactionSemaphore], out bool isLockHeld, (obj) =>
                    {
                        ObjectLockKey? lockKey = null;

                        var lockedObjects = GetOverlappingLocks(intention); //Find any existing locks on the given lock intention.

                        if (lockedObjects.Count == 0)
                        {
                            //No locks on the object exist - so add one to the local and class collections.
                            var lockedObject = new ObjectLock(_core, intention);
                            obj.Add(lockedObject);
                            lockedObjects.Add(lockedObject);
                        }

                        if (intention.Operation == LockOperation.Stability)
                        {
                            //This operation is blocked by: Delete.
                            var blockers = lockedObjects.SelectMany(o => o.Keys.Read((obj) => obj))
                                .Where(o => (o.Operation == LockOperation.Delete) && o.ProcessId != transaction.ProcessId).ToList();

                            if (blockers.Count == 0)
                            {
                                transaction.BlockedByKeys.Write((obj) => obj.Clear());

                                foreach (var lockedObject in lockedObjects)
                                {
                                    lockedObject.Hits++;

                                    if (lockedObject.Keys.Read((obj) => obj)
                                            .Any(o => o.ProcessId == transaction.ProcessId && o.Operation == intention.Operation))
                                    {
                                        //Do we really need to hand out multiple keys to the same object
                                        //  of the same type? I don't think we do. Just continue...
                                        continue;
                                    }

                                    lockKey = lockedObject.IssueSingleUseKey(transaction, intention);
                                    transaction.HeldLockKeys.Write((obj) => obj.Add(lockKey));
                                }

                                var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                                _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return lockKey;
                            }
                            else
                            {
                                transaction.BlockedByKeys.Write((obj) => obj.Clear());
                                transaction.BlockedByKeys.Write((obj) => obj.AddRange(blockers.Distinct()));
                            }
                        }
                        else if (intention.Operation == LockOperation.Read)
                        {
                            //This operation is blocked by: Read and Write.
                            var blockers = lockedObjects.SelectMany(o => o.Keys.Read((obj) => obj))
                                .Where(o => (o.Operation == LockOperation.Write || o.Operation == LockOperation.Delete)
                                && o.ProcessId != transaction.ProcessId).ToList();

                            if (blockers.Count == 0)
                            {
                                transaction.BlockedByKeys.Write((obj) => obj.Clear());

                                foreach (var lockedObject in lockedObjects)
                                {
                                    lockedObject.Hits++;

                                    if (lockedObject.Keys.Read((obj) => obj).Any(o
                                        => o.ProcessId == transaction.ProcessId && o.Operation == intention.Operation))
                                    {
                                        //Do we really need to hand out multiple keys to the same
                                        //  object of the same type? I don't think we do. Just continue...
                                        continue;
                                    }

                                    lockKey = lockedObject.IssueSingleUseKey(transaction, intention);
                                    transaction.HeldLockKeys.Write((obj) => obj.Add(lockKey));
                                }

                                var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                                _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return lockKey;
                            }
                            else
                            {
                                transaction.BlockedByKeys.Write((obj) => obj.Clear());
                                transaction.BlockedByKeys.Write((obj) => obj.AddRange(blockers.Distinct()));
                            }
                        }
                        else if (intention.Operation == LockOperation.Write)
                        {
                            //This operation is blocked by: Read, Write, Delete.
                            var blockers = lockedObjects.SelectMany(o => o.Keys.Read((obj) => obj))
                                .Where(o => o.Operation != LockOperation.Stability && o.ProcessId != transaction.ProcessId).ToList();

                            if (blockers.Count == 0)
                            {
                                transaction.BlockedByKeys.Write((obj) => obj.Clear());

                                foreach (var lockedObject in lockedObjects)
                                {
                                    lockedObject.Hits++;

                                    if (lockedObject.Keys.Read((obj) => obj.Any(o => o.ProcessId == transaction.ProcessId
                                    && o.Operation == intention.Operation)))
                                    {
                                        //Do we really need to hand out multiple keys to the same object of the same type?
                                        //I don't think we do.
                                        continue;
                                    }

                                    lockKey = lockedObject.IssueSingleUseKey(transaction, intention);
                                    transaction.HeldLockKeys.Write((obj) => obj.Add(lockKey));
                                }

                                var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                                _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return lockKey;
                            }
                            else
                            {
                                transaction.BlockedByKeys.Write((obj) => obj.Clear());
                                transaction.BlockedByKeys.Write((obj) => obj.AddRange(blockers.Distinct()));
                            }
                        }
                        else if (intention.Operation == LockOperation.Delete)
                        {
                            //This operation is blocked by: Everything
                            var blockers = lockedObjects.SelectMany(o => o.Keys.Read((obj) => obj))
                                .Where(o => o.ProcessId != transaction.ProcessId).ToList();

                            if (blockers.Count == 0) //If there are no existing un-owned locks.
                            {
                                transaction.BlockedByKeys.Write((obj) => obj.Clear());

                                foreach (var lockedObject in lockedObjects)
                                {
                                    lockedObject.Hits++;

                                    if (lockedObject.Keys.Read((obj) => obj.Any(
                                        o => o.ProcessId == transaction.ProcessId && o.Operation == intention.Operation)))
                                    {
                                        //Do we really need to hand out multiple keys to the same object of the same type?
                                        //I don't think we do.
                                        continue;
                                    }

                                    lockKey = lockedObject.IssueSingleUseKey(transaction, intention);

                                    transaction.HeldLockKeys.Write((obj) => obj.Add(lockKey));
                                }

                                var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                                _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return lockKey;
                            }
                            else
                            {
                                transaction.BlockedByKeys.Write((obj) => obj.Clear());
                                transaction.BlockedByKeys.Write((obj) => obj.AddRange(blockers.Distinct()));
                            }
                        }

                        transaction.BlockedByKeys.Read((obj) =>
                        {
                            if (obj.Count != 0)
                            {
                                _transactionWaitingForLocks.Read((txWaitingForLocks) =>
                                {
                                    //Get a list of all valid transactions.
                                    var waitingTransactions = txWaitingForLocks.Keys.Where(o => o.IsDeadlocked == false);

                                    //Get a list of transactions that are blocked by the current transaction.
                                    var blockedByMe = waitingTransactions.Where(
                                        o => o.BlockedByKeys.ReadNullable((obj) => obj.Where(
                                            k => k.ProcessId == transaction.ProcessId).Any())).ToList();

                                    foreach (var blocked in blockedByMe)
                                    {
                                        //Check to see if the current transaction is waiting
                                        //  on any of those blocked transaction (circular reference).

                                        if (obj.Any(o => o.ProcessId == blocked.ProcessId))
                                        {
                                            var explanation = GetDeadlockExplanation(transaction, txWaitingForLocks, intention, blockedByMe);

                                            transaction.SetDeadlocked();

                                            throw new KbDeadlockException($"Deadlock occurred, transaction for process {transaction.ProcessId} is being terminated.", explanation.ToString());
                                        }
                                    }
                                });
                            }
                        });
                        return null;
                    });

                    if (acquiredLockKey != null)
                    {
                        return acquiredLockKey;
                    }

                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Interactions.Management.LogManager.Error($"Failed to acquire lock for process {transaction.ProcessId}.", ex);
                throw;
            }
            finally
            {
                transaction.CurrentLockIntention = null;
                _transactionWaitingForLocks.Write((obj) => obj.Remove(transaction));
            }
        }

        private string GetDeadlockExplanation(Transaction transaction,
            Dictionary<Transaction, ObjectLockIntention> txWaitingForLocks,
            ObjectLockIntention intention, List<Transaction> blockedByMe)
        {
            var deadLockId = Guid.NewGuid().ToString();

            var explanation = new StringBuilder();

            explanation.AppendLine("Deadlock {");
            explanation.AppendLine($"    Id: {deadLockId}");
            explanation.AppendLine("    Blocking Transactions {");
            explanation.AppendLine($"        ProcessId: {transaction.ProcessId}");
            explanation.AppendLine($"        Operation: {transaction.TopLevelOperation}");
            //explanation.AppendLine($"        ReferenceCount: {transaction.ReferenceCount}"); //This causes a race condition.
            explanation.AppendLine($"        StartTime: {transaction.StartTime}");

            explanation.AppendLine("        Lock Intention {");
            explanation.AppendLine($"            ProcessId: {transaction.ProcessId}");
            explanation.AppendLine($"            Granularity: {intention.Granularity}");
            explanation.AppendLine($"            Operation: {intention.Operation}");
            explanation.AppendLine($"            Object: {intention.DiskPath}");
            explanation.AppendLine("        }");

            explanation.AppendLine("        Held Locks {");
            transaction.HeldLockKeys.Read((obj) =>
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
                //explanation.AppendLine($"        ReferenceCount: {waiter.ReferenceCount}"); //This causes a race condition.
                explanation.AppendLine($"        StartTime: {waiter.StartTime}");

                explanation.AppendLine("        Held Locks {");

                waiter.HeldLockKeys.Read((obj) =>
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
            if (string.IsNullOrEmpty(transaction.Session.QueryText) == false)
            {
                explanation.AppendLine($"    Query: {transaction.Session.QueryText}");
            }
            explanation.AppendLine("}");

            transaction.AddMessage(explanation.ToString(), KbConstants.KbMessageType.Deadlock);

            return explanation.ToString();
        }
    }
}
