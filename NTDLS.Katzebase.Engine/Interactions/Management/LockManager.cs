using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Instrumentation;
using NTDLS.Katzebase.Engine.Locking;
using NTDLS.Semaphore;
using System.Diagnostics;
using System.Text;
using static NTDLS.Katzebase.Api.KbConstants;
using static NTDLS.Katzebase.Shared.EngineConstants;

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
        private readonly KbInsensitiveDictionary<ObjectConcurrencyLock> _concurrentGrantLocks = new();

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
                _collection.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Remove(objectLock));
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for object [{objectLock.ToString()}].", ex);
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
        {
            return _pendingGrants.Read((pendingGrants) =>
                pendingGrants.ToDictionary(o => o.Value.Transaction.Snapshot(), o => o.Value.Intention));
        }

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
                var ptPendingGrantLock = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.PendingGrantLock, "Write");
                _pendingGrants.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (pendingGrants) =>
                {
                    ptPendingGrantLock?.StopAndAccumulate();
                    pendingGrants.Add(pendingGrantKey, new(transaction, intention));
                });
            }

            try
            {
                //We will loop until we either get the lock or an exception occurs (most likely a deadlock or cancelled transaction).
                while (true)
                {
                    //Lock the individual object semaphore, because we only allow an attempt for a lock on a single distinct file at a time.
                    var ptLockConcurrencyWait = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.LockConcurrencyWait);
                    if (concurrencyLock.Semaphore.Wait(1))
                    {
                        ptLockConcurrencyWait?.StopAndAccumulate();
                        try
                        {
                            var ptGrantedLockCache = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.GrantedLockCache, "Read");
                            if (transaction.GrantedLockCache.Read((obj) => obj.Contains(intention.Key)))
                            {
                                ptGrantedLockCache?.StopAndAccumulate();
                                //This transaction owns the lock, but it was created with a previous call to Acquire().
                                //  This means that the transaction has the key, but the caller will not be be provided
                                //  with it since modification by a non-creator caller would be dangerous.
                                //
                                //Additionally, we will not issue a new SingleUseKey for the lock because that would be wasteful.
                                return null;
                            }
                            ptGrantedLockCache?.StopAndAccumulate();

                            var ptAttemptLock = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.AttemptLock);
                            var lockKey = AttemptLock(transaction, intention);
                            ptAttemptLock?.StopAndAccumulate();

                            if (lockKey != null)
                            {
                                //We got a lock, record it and return the key to the caller.
                                var ptGrantedLockCacheWrite = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.GrantedLockCache, "Write");
                                transaction.GrantedLockCache.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Add(intention.Key));
                                ptGrantedLockCacheWrite?.StopAndAccumulate();
                                return lockKey;
                            }
                        }
                        finally
                        {
                            //Allow other threads to now attempt a lock on this file.
                            concurrencyLock.Semaphore.Release();
                        }
                    }
                    else
                    {
                        ptLockConcurrencyWait?.StopAndAccumulate();
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
                    var ptPendingGrantLock = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.PendingGrantLock, "Read");
                    _pendingGrants.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (pendingGrants) =>
                    {
                        ptPendingGrantLock?.StopAndAccumulate();
                        pendingGrants.Remove(pendingGrantKey);
                    });
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

                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Failed to rollback transaction for process [{transaction.ProcessId}] after lock wait timeout.", ex);
                    }

                    throw new KbTimeoutException($"Timeout exceeded while waiting on lock: [{intention.ToString()}]");
                }

                //Since _collection, tx.GrantedLockCache, tx.HeldLockKeys and tx.BlockedByKeys all use the critical
                //  section "Locking.CriticalSectionLockManagement", we will only need:
                return _collection.TryWriteAllNullable([transaction.TransactionSemaphore], out bool isLockHeld, (obj) =>
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
                        transaction.HeldLockKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Add(lockKey));

                        var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                        _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, lockWaitTime);
                        _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                        return lockKey;
                    }

                    #region Stability Lock.

                    if (intention.Operation == LockOperation.Stability)
                    {
                        //This operation is blocked by: Delete.
                        var blockers = lockedObjects.SelectMany(o => o.Keys.Read((obj) => obj))
                            .Where(o => (o.Operation == LockOperation.Delete) && o.ProcessId != transaction.ProcessId).ToList();

                        if (blockers.Count == 0)
                        {
                            transaction.BlockedByKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Clear());

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
                                transaction.HeldLockKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Add(lockKey));
                            }

                            var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, lockWaitTime);
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                            return lockKey;
                        }
                        else
                        {
                            transaction.BlockedByKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) =>
                            {
                                obj.Clear();
                                obj.AddRange(blockers.Distinct());
                            });
                        }
                    }

                    #endregion

                    #region Read Lock.

                    else if (intention.Operation == LockOperation.Read)
                    {
                        //This operation is blocked by: Write and Delete.
                        var blockers = lockedObjects.SelectMany(o => o.Keys.Read((obj) => obj))
                            .Where(o => (o.Operation == LockOperation.Write || o.Operation == LockOperation.Delete)
                            && o.ProcessId != transaction.ProcessId).ToList();

                        if (blockers.Count == 0)
                        {
                            transaction.BlockedByKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Clear());

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
                                transaction.HeldLockKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Add(lockKey));
                            }

                            var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, lockWaitTime);
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                            return lockKey;
                        }
                        else
                        {
                            transaction.BlockedByKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) =>
                            {
                                obj.Clear();
                                obj.AddRange(blockers.Distinct());
                            });
                        }
                    }

                    #endregion

                    #region Write Lock.

                    else if (intention.Operation == LockOperation.Write)
                    {
                        //This operation is blocked by: Read, Write, Delete.
                        var blockers = lockedObjects.SelectMany(o => o.Keys.Read((obj) => obj))
                            .Where(o => o.Operation != LockOperation.Stability && o.ProcessId != transaction.ProcessId).ToList();

                        if (blockers.Count == 0)
                        {
                            transaction.BlockedByKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Clear());

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
                                transaction.HeldLockKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Add(lockKey));
                            }

                            var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, lockWaitTime);
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                            return lockKey;
                        }
                        else
                        {
                            transaction.BlockedByKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) =>
                            {
                                obj.Clear();
                                obj.AddRange(blockers.Distinct());
                            });
                        }
                    }

                    #endregion

                    #region Delete Lock.

                    else if (intention.Operation == LockOperation.Delete)
                    {
                        //This operation is blocked by: Everything
                        var blockers = lockedObjects.SelectMany(o => o.Keys.Read((obj) => obj))
                            .Where(o => o.ProcessId != transaction.ProcessId).ToList();

                        if (blockers.Count == 0) //If there are no existing un-owned locks.
                        {
                            transaction.BlockedByKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Clear());

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

                                transaction.HeldLockKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Add(lockKey));
                            }

                            var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, lockWaitTime);
                            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                            return lockKey;
                        }
                        else
                        {
                            transaction.BlockedByKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) =>
                            {
                                obj.Clear();
                                obj.AddRange(blockers.Distinct());
                            });
                        }
                    }

                    #endregion

                    #region Deadlock Detection.

                    var ptDeadlockDetection = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.DeadlockDetection);
                    transaction.BlockedByKeys.Read((currentBlockedByKeys) =>
                    {
                        if (currentBlockedByKeys.Count != 0)
                        {
                            var ptPendingGrantLock = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.PendingGrantLock, "Read");
                            _pendingGrants.Read((pendingGrants) =>
                            {
                                ptPendingGrantLock?.StopAndAccumulate();

                                // Build a lookup of all non-deadlocked waiting transactions.
                                var waitingTransactions = pendingGrants
                                    .Where(o => o.Value.Transaction.IsDeadlocked == false)
                                    .Select(o => o.Value.Transaction)
                                    .Distinct()
                                    .ToDictionary(o => o.ProcessId);

                                // Deadlock detection using Depth-First Search (DFS) over the waits-for graph.
                                //
                                // The waits-for graph has one node per transaction and a directed edge A→B
                                // whenever transaction A is waiting on a lock held by transaction B.
                                // A deadlock is a cycle in this graph — every transaction in the cycle is
                                // permanently stuck waiting on the next one.
                                //
                                // We detect a cycle by starting at the current transaction's blockers and
                                // following edges as deep as possible before backtracking (DFS). If we ever
                                // arrive back at the current transaction's ProcessId, a cycle exists.
                                //
                                // Example — 3-party deadlock (A→B→C→A):
                                //   seed → push B            parent[B] = A
                                //   pop B  (B≠A)  →  push C  parent[C] = B
                                //   pop C  (C≠A)  →  push A  parent[A] = C
                                //   pop A  (A==A) →  deadlock! walk parent map: A←C←B←A → reverse → A→B→C→A
                                //
                                // The visited set prevents infinite loops when the graph contains a cycle
                                // that does not involve the current transaction: once a node is fully
                                // explored there is no value in visiting it again.
                                //
                                // The parent map records who pushed each node, enabling reconstruction of
                                // the full ordered cycle chain for the deadlock explanation.
                                var visited = new HashSet<ulong>();
                                var toVisit = new Stack<ulong>();

                                // parent[X] = Y means Y pushed X during traversal — used to reconstruct
                                // the cycle chain by walking backwards when a deadlock is detected.
                                var parent = new Dictionary<ulong, ulong>();

                                // Seed the search with every transaction that is currently blocking us.
                                foreach (var blockerKey in currentBlockedByKeys)
                                {
                                    var blockerPid = blockerKey.ProcessId;
                                    if (waitingTransactions.ContainsKey(blockerPid) && !parent.ContainsKey(blockerPid))
                                    {
                                        parent[blockerPid] = transaction.ProcessId;
                                        toVisit.Push(blockerPid);
                                    }
                                }

                                while (toVisit.Count > 0)
                                {
                                    var pid = toVisit.Pop();

                                    if (pid == transaction.ProcessId)
                                    {
                                        // Cycle detected. Walk the parent map backwards from the current
                                        // transaction to reconstruct the full ordered cycle chain, then reverse.
                                        // e.g. for A→B→C→A with parent={B:A, C:B, A:C}:
                                        //   walk: A → C → B → A  →  reversed: A → B → C → A
                                        var cycleChain = new List<ulong>();
                                        var cur = transaction.ProcessId;
                                        do
                                        {
                                            cycleChain.Add(cur);
                                            cur = parent[cur];
                                        } while (cur != transaction.ProcessId);
                                        cycleChain.Add(transaction.ProcessId); // close the loop
                                        cycleChain.Reverse();

                                        var explanation = GetDeadlockExplanation(transaction, pendingGrants, intention, cycleChain, waitingTransactions);

                                        transaction.SetDeadlocked();
                                        ptDeadlockDetection?.StopAndAccumulate();

                                        throw new KbDeadlockException(
                                            $"Deadlock occurred, transaction for process [{transaction.ProcessId}] is being terminated.",
                                            explanation.ToString());
                                    }

                                    if (!visited.Add(pid))
                                        continue; // Already explored every edge from this node.

                                    // Follow this transaction's own blockers to go one level deeper.
                                    if (waitingTransactions.TryGetValue(pid, out var nextTx))
                                    {
                                        nextTx.BlockedByKeys.Read((blockedBy) =>
                                        {
                                            foreach (var key in blockedBy)
                                            {
                                                if (!visited.Contains(key.ProcessId) && !parent.ContainsKey(key.ProcessId))
                                                {
                                                    parent[key.ProcessId] = pid;
                                                    toVisit.Push(key.ProcessId);
                                                }
                                            }
                                        });
                                    }
                                }
                            });
                        }
                    });
                    ptDeadlockDetection?.StopAndAccumulate();

                    #endregion

                    return null;
                }); //If we got a lock, return its key.
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], object: [{intention.ToString()}].", ex);
                throw;
            }
        }

        private static string GetDeadlockExplanation(
            Transaction transaction,
            Dictionary<Guid, ObjectPendingLockIntention> txWaitingForLocks,
            ObjectLockIntention intention,
            List<ulong> cycleChain,
            Dictionary<ulong, Transaction> waitingTransactions)
        {
            var explanation = new StringBuilder();

            explanation.AppendLine("Deadlock {");
            explanation.AppendLine($"    Id: {Guid.NewGuid()}");
            explanation.AppendLine($"    Cycle: {string.Join(" → ", cycleChain)}");

            // Full details for the transaction being terminated.
            explanation.AppendLine("    Victim {");
            explanation.AppendLine($"        ProcessId: {transaction.ProcessId}");
            explanation.AppendLine($"        Operation: {transaction.TopLevelOperation}");
            explanation.AppendLine($"        ReferenceCount: {transaction.ReferenceCount}");
            explanation.AppendLine($"        StartTime: {transaction.StartTime}");
            explanation.AppendLine("        Lock Intention {");
            explanation.AppendLine($"            Granularity: {intention.Granularity}");
            explanation.AppendLine($"            Operation: {intention.Operation}");
            explanation.AppendLine($"            Object: {intention.DiskPath}");
            explanation.AppendLine("        }");
            explanation.AppendLine("        Held Locks {");
            transaction.HeldLockKeys.Read((obj) =>
            {
                foreach (var key in obj)
                    explanation.AppendLine($"            {key.ToString()}");
            });
            explanation.AppendLine("        }");
            explanation.AppendLine("        Awaiting Locks {");
            foreach (var waitingFor in txWaitingForLocks.Where(o => o.Value.Transaction == transaction))
                explanation.AppendLine($"            {waitingFor.Value.Intention.ToString()}");
            explanation.AppendLine("        }");
            explanation.AppendLine("    }");

            // All other transactions in the cycle in order, excluding the victim (first entry)
            // and the closing duplicate (last entry) from the chain.
            var participants = cycleChain.Skip(1).SkipLast(1).ToList();
            if (participants.Count > 0)
            {
                explanation.AppendLine("    Cycle Participants {");
                foreach (var participantPid in participants)
                {
                    if (!waitingTransactions.TryGetValue(participantPid, out var participant))
                        continue;

                    explanation.AppendLine("        Transaction {");
                    explanation.AppendLine($"            ProcessId: {participant.ProcessId}");
                    explanation.AppendLine($"            Operation: {participant.TopLevelOperation}");
                    explanation.AppendLine($"            ReferenceCount: {participant.ReferenceCount}");
                    explanation.AppendLine($"            StartTime: {participant.StartTime}");
                    explanation.AppendLine("            Held Locks {");
                    participant.HeldLockKeys.Read((obj) =>
                    {
                        foreach (var key in obj)
                            explanation.AppendLine($"                {key.ToString()}");
                    });
                    explanation.AppendLine("            }");
                    explanation.AppendLine("            Awaiting Locks {");
                    foreach (var waitingFor in txWaitingForLocks.Where(o => o.Value.Transaction == participant))
                        explanation.AppendLine($"                {waitingFor.Value.Intention.ToString()}");
                    explanation.AppendLine("            }");
                    explanation.AppendLine("        }");
                }
                explanation.AppendLine("    }");
            }

            if (!string.IsNullOrEmpty(transaction.Session.CurrentQueryText()))
                explanation.AppendLine($"    Query: {transaction.Session.QueryTextStack}");

            explanation.AppendLine("}");

            transaction.AddMessage(explanation.ToString(), KbMessageType.Deadlock);

            return explanation.ToString();
        }
    }
}
