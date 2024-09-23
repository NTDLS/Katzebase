using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Locking;
using NTDLS.Semaphore;
using System.Text;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Internal core class methods for locking, reading, writing and managing tasks related to locking.
    /// </summary>
    internal class LockManager
    {
        private readonly EngineCore _core;

        /// <summary>
        /// Collection of all locks across all transactions.
        /// </summary>
        private readonly OptimisticCriticalResource<List<ObjectLock>> _collection;

        /// <summary>
        /// Used to ensure that across all transactions, we only allow 1 thread at a time to lock any individual files.
        /// This does not block the the same transaction or other transactions from locking other files.
        /// Other transactions can also lock the same file too, they just have to wait for the pending grant.
        /// </summary>
        private static readonly KbInsensitiveDictionary<ObjectConcurrencyLock> _concurrentGrantLocks = new();

        /// <summary>
        //We keep track of all files/transactions that are waiting on locks for a few reasons:
        // (1) When we suspect a deadlock we know what all transactions are potentially involved.
        // (2) We are safe to poke around those transaction's properties because we know their threads are working in this function.
        // (2.1) Point number 2 is now only half true, the transaction is working in this function - but it can be using multiple
        //          threads to access multiple files. So we know the transaction is still present in the engine collection, but other
        //          transactions may be changed.
        /// </summary>
        private readonly OptimisticCriticalResource<Dictionary<Guid, ObjectPendingLockIntention>> _pendingGrants;

        internal LockManager(EngineCore core)
        {
            _core = core;

            try
            {
                _core = core;
                _collection = new(core.LockManagementSemaphore);
                _pendingGrants = new(core.LockManagementSemaphore);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate lock manager.", ex);
                throw;
            }
        }

        internal void Release(ObjectLock objectLock)
        {
            try
            {
                _collection.Write((obj) => obj.Remove(objectLock));
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to remove lock.", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns a set (if any) of existing locks that that would conflict with the given lock intention.
        /// </summary>
        /// <param name="intention"></param>
        /// <returns></returns>
        internal HashSet<ObjectLock> GetOverlappingLocks(ObjectLockIntention intention)
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
            => _pendingGrants.Read((obj) =>
                obj.ToDictionary(o => o.Value.Transaction.Snapshot(), o => o.Value.Intention)
            );

        internal ObjectLockKey? Acquire(Transaction transaction, ObjectLockIntention intention)
        {
            ObjectConcurrencyLock? concurrencyLock = null;

            var pendingGrantKey = Guid.NewGuid();

            lock (_concurrentGrantLocks)
            {
                // Produces a semaphore that is used to ensure that we do not simultaneously operate on any single file.
                if (_concurrentGrantLocks.TryGetValue(intention.Key, out concurrencyLock))
                {
                    //There are other threads currently waiting for a lock on this file.
                    concurrencyLock.ReferenceCount++;
                }
                else
                {
                    //This is the first thread in line for a lock on this file.
                    concurrencyLock = new ObjectConcurrencyLock();
                    _concurrentGrantLocks.Add(intention.Key, concurrencyLock);
                }

                //Record that we are waiting on the grant. This is used for deadlock detection.
                _pendingGrants.Write((o) => o.Add(pendingGrantKey, new(transaction, intention)));
            }

            try
            {
                //We will loop until we either get the lock or an exception occurs (most likely a deadlock or cancelled transaction).
                while (true)
                {
                    //Lock the individual object semaphore, because we only allow an attempt for a lock on a single distinct file at a time.
                    if (concurrencyLock.Semaphore.Wait(1))
                    {
                        try
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

                            var lockKey = AttemptLock(transaction, intention);
                            if (lockKey != null)
                            {
                                //We got a lock, record it and return the key to the caller.
                                transaction.GrantedLockCache.Write((obj) => obj.Add(intention.Key));
                                return lockKey;
                            }
                        }
                        finally
                        {
                            //Allow other threads to now attempt a lock on this file.
                            concurrencyLock.Semaphore.Release();
                        }
                    }
                }
            }
            finally
            {
                lock (_concurrentGrantLocks)
                {
                    //Decrement this threads lock on the file and remove it from the collection if we are the last one.
                    _concurrentGrantLocks[intention.Key].ReferenceCount--;
                    if (_concurrentGrantLocks[intention.Key].ReferenceCount == 0)
                    {
                        _concurrentGrantLocks.Remove(intention.Key);
                    }

                    //Let other transactions know that we are no longer waiting on this lock.
                    _pendingGrants.Write((obj) => obj.Remove(pendingGrantKey));
                }
            }
        }

        private ObjectLockKey? AttemptLock(Transaction transaction, ObjectLockIntention intention)
        {
            try
            {
                transaction.EnsureActive();

                if (_core.Settings.LockWaitTimeoutSeconds > 0 //Infinite lock expiration.
                    && (DateTime.UtcNow - intention.CreationTime).TotalSeconds > _core.Settings.LockWaitTimeoutSeconds)
                {
                    var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                    _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, lockWaitTime);
                    _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);
                    transaction.Rollback();
                    throw new KbTimeoutException($"Timeout exceeded while waiting on lock: {intention.ToString()}");
                }

                //Since _collection, tx.GrantedLockCache, tx.HeldLockKeys and tx.BlockedByKeys all use the critical
                //  section "Locking.CriticalSectionLockManagement", we will only need:
                return _collection.TryWriteAll([transaction.TransactionSemaphore], out bool isLockHeld, (obj) =>
                {
                    ObjectLockKey? lockKey = null;

                    var lockedObjects = GetOverlappingLocks(intention); //Find any existing locks on the given lock intention.

                    if (lockedObjects.Count == 0)
                    {
                        //No locks on the object exist - so add one to the local and class collections.
                        var lockedObject = new ObjectLock(_core, intention);
                        obj.Add(lockedObject);
                        lockedObjects.Add(lockedObject);

                        lockKey = lockedObject.IssueSingleUseKey(transaction, intention);
                        transaction.HeldLockKeys.Write((obj) => obj.Add(lockKey));

                        var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                        _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, lockWaitTime);
                        _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                        return lockKey;
                    }

                    #region Stability.
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
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, lockWaitTime);
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                            return lockKey;
                        }
                        else
                        {
                            transaction.BlockedByKeys.Write((obj) => obj.Clear());
                            transaction.BlockedByKeys.Write((obj) => obj.AddRange(blockers.Distinct()));
                        }
                    }
                    #endregion

                    #region Read.
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
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, lockWaitTime);
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                            return lockKey;
                        }
                        else
                        {
                            transaction.BlockedByKeys.Write((obj) => obj.Clear());
                            transaction.BlockedByKeys.Write((obj) => obj.AddRange(blockers.Distinct()));
                        }
                    }
                    #endregion

                    #region Write.
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
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, lockWaitTime);
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                            return lockKey;
                        }
                        else
                        {
                            transaction.BlockedByKeys.Write((obj) => obj.Clear());
                            transaction.BlockedByKeys.Write((obj) => obj.AddRange(blockers.Distinct()));
                        }
                    }
                    #endregion

                    #region Delete.
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
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, lockWaitTime);
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                            return lockKey;
                        }
                        else
                        {
                            transaction.BlockedByKeys.Write((obj) => obj.Clear());
                            transaction.BlockedByKeys.Write((obj) => obj.AddRange(blockers.Distinct()));
                        }
                    }
                    #endregion

                    #region Deadlock detection.

                    transaction.BlockedByKeys.Read((obj) =>
                    {
                        if (obj.Count != 0)
                        {
                            _pendingGrants.Read((waitingLocks) =>
                            {
                                var waitingTransactions = waitingLocks
                                    .Where(o => o.Value.Transaction.IsDeadlocked == false)
                                    .Select(o => o.Value.Transaction).ToList();

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
                                        var explanation = GetDeadlockExplanation(transaction, waitingLocks, intention, blockedByMe);

                                        transaction.SetDeadlocked();

                                        throw new KbDeadlockException($"Deadlock occurred, transaction for process {transaction.ProcessId} is being terminated.", explanation.ToString());
                                    }
                                }
                            });
                        }
                    });

                    #endregion

                    return null;
                }); //If we got a lock, return its key.
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to acquire lock for process {transaction.ProcessId}.", ex);
                throw;
            }
        }

        private string GetDeadlockExplanation(Transaction transaction,
            Dictionary<Guid, ObjectPendingLockIntention> txWaitingForLocks,
            ObjectLockIntention intention, List<Transaction> blockedByMe)
        {
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
            foreach (var waitingFor in txWaitingForLocks.Where(o => o.Value.Transaction == transaction))
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
                foreach (var waitingFor in txWaitingForLocks.Where(o => o.Value.Transaction == waiter))
                {
                    explanation.AppendLine($"            {waitingFor.Value.Intention}");
                }
                explanation.AppendLine("        }");
            }
            explanation.AppendLine("    }");
            if (string.IsNullOrEmpty(transaction.Session.QueryText) == false)
            {
                explanation.AppendLine($"    Query: {transaction.Session.QueryText}");
            }
            explanation.AppendLine("}");

            transaction.AddMessage(explanation.ToString(), KbMessageType.Deadlock);

            return explanation.ToString();
        }
    }
}
