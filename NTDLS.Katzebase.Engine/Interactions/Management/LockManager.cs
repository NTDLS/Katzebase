using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Locking;
using NTDLS.Katzebase.Shared;
using NTDLS.Semaphore;
using System.Collections.Concurrent;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Internal core class methods for locking, reading, writing and managing tasks related to locking.
    /// </summary>
    internal class LockManager
    {
        class WaitingObjectLockIntention
        {
            public Transaction Transaction { get; set; }
            public ObjectLockIntention Intention { get; set; }

            public WaitingObjectLockIntention(Transaction transaction, ObjectLockIntention intention)
            {
                Transaction = transaction;
                Intention = intention;
            }
        }

        private readonly OptimisticCriticalResource<List<ObjectLock>> _collection;
        private readonly OptimisticCriticalResource<Dictionary<string /*TransactionId:FilePath*/, WaitingObjectLockIntention>> _transactionWaitingForLocks;
        private readonly EngineCore _core;

        internal LockManager(EngineCore core)
        {
            _core = core;

            try
            {
                _core = core;
                _collection = new(core.LockManagementSemaphore);
                _transactionWaitingForLocks = new(core.LockManagementSemaphore);
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
                CrudeInstrumentation.Witness(() => _collection.Write((obj) => obj.Remove(objectLock)));
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

        private static readonly Dictionary<string, object> _concurrentFileLocks = new();

        internal Dictionary<TransactionSnapshot, ObjectLockIntention> SnapshotWaitingTransactions()
        {
            throw new NotImplementedException();
            //=> _transactionWaitingForLocks.Read((obj) => obj.ToDictionary(o => o.Key.Snapshot(), o => o.Value));
        }

        internal ObjectLockKey? Acquire(Transaction transaction, ObjectLockIntention intention)
        {
            object? concurrencyLock = null;

            lock (_concurrentFileLocks)
            {
                /// Produces a lock object that is used to ensure that we do not operate on the same file at the same time from different threads.
                if (_concurrentFileLocks.TryGetValue(intention.Key, out concurrencyLock) == false)
                {
                    concurrencyLock = new object();
                    _concurrentFileLocks.Add(intention.Key, concurrencyLock);
                }
            }

            lock (concurrencyLock)
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

                    var lockKey = AcquireInternal(transaction, intention);

                    CrudeInstrumentation.Witness(() => transaction.GrantedLockCache.Write((obj) => obj.Add(intention.Key)));

                    return lockKey;
                }
                finally
                {
                    lock (_concurrentFileLocks)
                    {
                        _concurrentFileLocks.Remove(intention.Key);
                    }
                }
            }
        }

        private ObjectLockKey AcquireInternal(Transaction transaction, ObjectLockIntention intention)
        {
            string waitingKey = $"{transaction.Id}:{intention.Key}";

            try
            {
                //We keep track of all transactions that are waiting on locks for a few reasons:
                // (1) When we suspect a deadlock we know what all transactions are potentially involved.
                // (2) We are safe to poke around those transaction's properties because we know their threads are working in this function.
                CrudeInstrumentation.Witness(() => _transactionWaitingForLocks.Write((obj) =>
                {

                    if (obj.Count > 0)
                    {

                    }

                    obj.Add(waitingKey, new(transaction, intention));

                }));

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
                                CrudeInstrumentation.Witness(() => transaction.BlockedByKeys.Write((obj) => obj.Clear()));

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
                                    CrudeInstrumentation.Witness(() => transaction.HeldLockKeys.Write((obj) => obj.Add(lockKey)));
                                }

                                var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                                _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return lockKey;
                            }
                            else
                            {
                                CrudeInstrumentation.Witness(() => transaction.BlockedByKeys.Write((obj) => obj.Clear()));
                                CrudeInstrumentation.Witness(() => transaction.BlockedByKeys.Write((obj) => obj.AddRange(blockers.Distinct())));
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
                                CrudeInstrumentation.Witness(() => transaction.BlockedByKeys.Write((obj) => obj.Clear()));

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
                                    CrudeInstrumentation.Witness(() => transaction.HeldLockKeys.Write((obj) => obj.Add(lockKey)));
                                }

                                var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                                _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return lockKey;
                            }
                            else
                            {
                                CrudeInstrumentation.Witness(() => transaction.BlockedByKeys.Write((obj) => obj.Clear()));
                                CrudeInstrumentation.Witness(() => transaction.BlockedByKeys.Write((obj) => obj.AddRange(blockers.Distinct())));
                            }
                        }
                        else if (intention.Operation == LockOperation.Write)
                        {
                            //This operation is blocked by: Read, Write, Delete.
                            var blockers = lockedObjects.SelectMany(o => o.Keys.Read((obj) => obj))
                                .Where(o => o.Operation != LockOperation.Stability && o.ProcessId != transaction.ProcessId).ToList();

                            if (blockers.Count == 0)
                            {
                                CrudeInstrumentation.Witness(() => transaction.BlockedByKeys.Write((obj) => obj.Clear()));

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
                                    CrudeInstrumentation.Witness(() => transaction.HeldLockKeys.Write((obj) => obj.Add(lockKey)));
                                }

                                var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                                _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return lockKey;
                            }
                            else
                            {
                                CrudeInstrumentation.Witness(() => transaction.BlockedByKeys.Write((obj) => obj.Clear()));
                                CrudeInstrumentation.Witness(() => transaction.BlockedByKeys.Write((obj) => obj.AddRange(blockers.Distinct())));
                            }
                        }
                        else if (intention.Operation == LockOperation.Delete)
                        {
                            //This operation is blocked by: Everything
                            var blockers = lockedObjects.SelectMany(o => o.Keys.Read((obj) => obj))
                                .Where(o => o.ProcessId != transaction.ProcessId).ToList();

                            if (blockers.Count == 0) //If there are no existing un-owned locks.
                            {
                                CrudeInstrumentation.Witness(() => transaction.BlockedByKeys.Write((obj) => obj.Clear()));

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

                                    CrudeInstrumentation.Witness(() => transaction.HeldLockKeys.Write((obj) => obj.Add(lockKey)));
                                }

                                var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                                _core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                _core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return lockKey;
                            }
                            else
                            {
                                CrudeInstrumentation.Witness(() => transaction.BlockedByKeys.Write((obj) => obj.Clear()));
                                CrudeInstrumentation.Witness(() => transaction.BlockedByKeys.Write((obj) => obj.AddRange(blockers.Distinct())));
                            }
                        }

                        transaction.BlockedByKeys.Read((obj) =>
                        {
                            if (obj.Count != 0)
                            {
                                _transactionWaitingForLocks.Read((txsWaitingForLocks) =>
                                {
                                    var waitingTransactions = txsWaitingForLocks
                                        .Where(o => o.Value.Transaction.IsDeadlocked == false)
                                        .Select(o => o.Value.Transaction).ToList();

                                    //Get a list of all valid transactions.
                                    //var waitingTransactions = txWaitingForLocks.Keys.Where(o => o.IsDeadlocked == false);

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
                                            var explanation = GetDeadlockExplanation(transaction, txsWaitingForLocks, intention, blockedByMe);

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
                LogManager.Error($"Failed to acquire lock for process {transaction.ProcessId}.", ex);
                throw;
            }
            finally
            {
                transaction.CurrentLockIntention = null;
                CrudeInstrumentation.Witness(() => _transactionWaitingForLocks.Write((obj) => obj.Remove(waitingKey)));
            }
        }

        private string GetDeadlockExplanation(Transaction transaction,
            Dictionary<string /*TransactionId:FilePath*/, WaitingObjectLockIntention> txWaitingForLocks,
            ObjectLockIntention intention, List<Transaction> blockedByMe)
        {
            return "";
            /*
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
            */
        }

    }
}
