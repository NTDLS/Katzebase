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
        /// Collection of all locks across all transactions, indexed so that overlap checks do not need to scan every lock.
        /// </summary>
        private readonly OptimisticCriticalResource<LockTable> _collection;

        /// <summary>
        //We keep track of all files/transactions that are waiting on locks for a few reasons:
        // (1) When we suspect a deadlock we know what all transactions are potentially involved.
        // (2) We are safe to poke around those transaction's properties because we know their threads are working in this function.
        // (2.1) Point number 2 is now only half true, the transaction is working in this function - but it can be using multiple
        //          threads to access multiple files. So we know the transaction is still present in the engine collection, but other
        //          transactions may be changed.
        //Only requests that are actually blocked are registered here, requests that are granted on their first attempt never wait.
        /// </summary>
        private readonly OptimisticCriticalResource<Dictionary<Guid, ObjectPendingLockIntention>> _pendingGrants;

        /// <summary>
        /// Incremented every time a lock key is turned in. Blocked requests sleep until this changes rather than
        /// spinning on the lock table, which previously starved every other transaction of the lock management semaphore.
        /// </summary>
        private long _releaseGeneration;
        private int _waiterCount;
        private readonly object _releaseSignal = new();

        /// <summary>
        /// Blocked requests wake at least this often to re-run deadlock detection, lock wait timeouts and cancellation checks.
        /// </summary>
        private const int BlockedRetryIntervalMs = 25;

        private enum AttemptResult
        {
            /// <summary>
            /// A new lock key was issued to the transaction.
            /// </summary>
            Granted,
            /// <summary>
            /// The transaction already holds keys that satisfy the intention (e.g. a covering schema/path lock).
            /// </summary>
            AlreadyHeld,
            /// <summary>
            /// Another transaction holds a conflicting key.
            /// </summary>
            Blocked,
            /// <summary>
            /// The lock table or transaction critical sections could not be obtained, retry shortly.
            /// </summary>
            Busy
        }

        private class LockRequest(Transaction transaction, ObjectLockIntention intention)
        {
            public Transaction Transaction { get; } = transaction;
            public ObjectLockIntention Intention { get; } = intention;
            public ObjectLockKey? LockKey { get; set; }
            /// <summary>
            /// Set once the request has been registered in _pendingGrants (after its first blocked attempt).
            /// </summary>
            public Guid? PendingGrantKey { get; set; }
        }

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
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for object [{objectLock}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Wakes any blocked lock requests so they can re-attempt their lock.
        /// </summary>
        internal void NotifyLockReleased()
        {
            Interlocked.Increment(ref _releaseGeneration);

            if (Volatile.Read(ref _waiterCount) > 0)
            {
                lock (_releaseSignal)
                {
                    Monitor.PulseAll(_releaseSignal);
                }
            }
        }

        private void WaitForLockRelease(long observedGeneration)
        {
            Interlocked.Increment(ref _waiterCount);
            try
            {
                lock (_releaseSignal)
                {
                    //If a lock was released after our attempt, then don't wait - just try again.
                    if (Interlocked.Read(ref _releaseGeneration) == observedGeneration)
                    {
                        Monitor.Wait(_releaseSignal, BlockedRetryIntervalMs);
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref _waiterCount);
            }
        }

        /// <summary>
        /// Returns true if a lock key with the operation [heldOperation] (held by another transaction)
        /// prevents a lock with the operation [requestedOperation] from being granted.
        /// </summary>
        private static bool IsBlockedBy(LockOperation requestedOperation, LockOperation heldOperation)
        {
            return requestedOperation switch
            {
                //Stability is blocked by: Delete.
                LockOperation.Stability => heldOperation == LockOperation.Delete,
                //Read is blocked by: Write and Delete.
                LockOperation.Read => heldOperation == LockOperation.Write || heldOperation == LockOperation.Delete,
                //Write is blocked by: Read, Write, Delete.
                LockOperation.Write => heldOperation != LockOperation.Stability,
                //Delete is blocked by: Everything.
                LockOperation.Delete => true,
                _ => throw new NotImplementedException($"Lock operation is not implemented: [{requestedOperation}].")
            };
        }

        internal Dictionary<TransactionSnapshot, ObjectLockIntention> SnapshotWaitingTransactions()
        {
            return _pendingGrants.Read((pendingGrants) =>
                pendingGrants.ToDictionary(o => o.Value.Transaction.Snapshot(), o => o.Value.Intention));
        }

        internal ObjectLockKey? Acquire(Transaction transaction, ObjectLockIntention intention)
        {
            transaction.EnsureActive();

            //This transaction was already granted this exact lock by a previous call to Acquire().
            //  The transaction has the key, but the caller will not be be provided with it since
            //  modification by a non-creator caller would be dangerous.
            var ptGrantedLockCache = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.GrantedLockCache, "Read");
            bool isCached = transaction.GrantedLockCache.ContainsKey(intention.Key);
            ptGrantedLockCache?.StopAndAccumulate();
            if (isCached)
            {
                return null;
            }

            //The most common case: the transaction already holds a lock that covers this one (e.g. a document within a
            //  schema that the transaction has already locked). This only needs a shared read of the lock table.
            if (IsSatisfiedByHeldLocks(transaction, intention))
            {
                transaction.GrantedLockCache.TryAdd(intention.Key, 0);
                return null;
            }

            var request = new LockRequest(transaction, intention);
            var spinWait = new SpinWait();

            try
            {
                //We will loop until we either get the lock or an exception occurs (most likely a deadlock, timeout or cancelled transaction).
                while (true)
                {
                    var observedGeneration = Interlocked.Read(ref _releaseGeneration);

                    var ptAttemptLock = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.AttemptLock);
                    var result = AttemptLock(request);
                    ptAttemptLock?.StopAndAccumulate();

                    switch (result)
                    {
                        case AttemptResult.Granted:
                            transaction.GrantedLockCache.TryAdd(intention.Key, 0);
                            RecordLockWait(intention);
                            return request.LockKey;

                        case AttemptResult.AlreadyHeld:
                            //Every overlapping lock is already owned by this transaction with the same operation — a
                            //  coarser-granularity lock (e.g. Path) covers this request. No new key is issued.
                            transaction.GrantedLockCache.TryAdd(intention.Key, 0);
                            RecordLockWait(intention);
                            return null;

                        case AttemptResult.Busy:
                            //The lock table or this transaction's critical section is momentarily held by another thread.
                            spinWait.SpinOnce();
                            break;

                        case AttemptResult.Blocked:
                            var ptLockWait = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.LockConcurrencyWait);
                            WaitForLockRelease(observedGeneration);
                            ptLockWait?.StopAndAccumulate();
                            break;
                    }
                }
            }
            finally
            {
                if (request.PendingGrantKey is Guid pendingGrantKey)
                {
                    //Let other transactions know that we are no longer waiting on this lock.
                    var ptPendingGrantLock = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.PendingGrantLock, "Write");
                    _pendingGrants.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (pendingGrants) => pendingGrants.Remove(pendingGrantKey));
                    ptPendingGrantLock?.StopAndAccumulate();
                }
            }
        }

        private void RecordLockWait(ObjectLockIntention intention)
        {
            var lockWaitTime = (DateTime.UtcNow - intention.CreationTime).TotalMilliseconds;
            _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, lockWaitTime);
            if (_core.Settings.HealthMonitoringInstanceLevelEnabled)
            {
                _core.Health.IncrementContinuous(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);
            }
        }

        /// <summary>
        /// Determines, using only a shared read of the lock table, whether the transaction already holds keys of the same operation on
        /// every lock that overlaps the intention and no other transaction holds a conflicting key. This is exactly the condition in
        /// which AttemptLock() would grant the request without issuing a new key, so the exclusive path can be skipped entirely.
        /// </summary>
        private bool IsSatisfiedByHeldLocks(Transaction transaction, ObjectLockIntention intention)
        {
            return _collection.Read((table) =>
            {
                var overlappingLocks = table.GetOverlappingLocks(intention);
                if (overlappingLocks.Count == 0)
                {
                    return false; //A new lock would need to be created.
                }

                foreach (var overlappingLock in overlappingLocks)
                {
                    bool holdsSameOperation = false;

                    foreach (var key in overlappingLock.Keys.Read((keys) => keys))
                    {
                        if (key.ProcessId == transaction.ProcessId)
                        {
                            holdsSameOperation |= key.Operation == intention.Operation;
                        }
                        else if (IsBlockedBy(intention.Operation, key.Operation))
                        {
                            return false;
                        }
                    }

                    if (!holdsSameOperation)
                    {
                        return false;
                    }
                }

                foreach (var overlappingLock in overlappingLocks)
                {
                    overlappingLock.IncrementHits();
                }

                return true;
            });
        }

        private AttemptResult AttemptLock(LockRequest request)
        {
            var transaction = request.Transaction;
            var intention = request.Intention;

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

                var result = AttemptResult.Busy;

                //Since _collection, tx.HeldLockKeys and tx.BlockedByKeys all use the critical
                //  section "Locking.CriticalSectionLockManagement", we will only need:
                _collection.TryWriteAllNullable([transaction.TransactionSemaphore], out bool isLockHeld, (table) =>
                {
                    var lockedObjects = table.GetOverlappingLocks(intention); //Find any existing locks on the given lock intention.

                    if (lockedObjects.Count == 0)
                    {
                        //No locks on the object exist - so add one to the local and class collections.
                        var lockedObject = new ObjectLock(_core, intention);
                        table.Add(lockedObject);

                        var lockKey = lockedObject.IssueSingleUseKey(transaction, intention);
                        transaction.HeldLockKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Add(lockKey));
                        transaction.BlockedByKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Clear());

                        request.LockKey = lockKey;
                        result = AttemptResult.Granted;
                        return lockKey;
                    }

                    var blockers = lockedObjects.SelectMany(o => o.Keys.Read((obj) => obj))
                        .Where(o => o.ProcessId != transaction.ProcessId && IsBlockedBy(intention.Operation, o.Operation))
                        .Distinct().ToList();

                    if (blockers.Count == 0)
                    {
                        transaction.BlockedByKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Clear());

                        foreach (var lockedObject in lockedObjects)
                        {
                            lockedObject.IncrementHits();

                            if (lockedObject.Keys.Read((obj) => obj.Any(o => o.ProcessId == transaction.ProcessId && o.Operation == intention.Operation)))
                            {
                                //Do we really need to hand out multiple keys to the same object of the same type? I don't think we do.
                                continue;
                            }

                            var lockKey = lockedObject.IssueSingleUseKey(transaction, intention);
                            transaction.HeldLockKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) => obj.Add(lockKey));
                            request.LockKey = lockKey;
                        }

                        result = request.LockKey != null ? AttemptResult.Granted : AttemptResult.AlreadyHeld;
                        return request.LockKey;
                    }

                    transaction.BlockedByKeys.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (obj) =>
                    {
                        obj.Clear();
                        obj.AddRange(blockers);
                    });

                    //Record that we are waiting on the grant. This is used for deadlock detection (both ours and other transactions').
                    if (request.PendingGrantKey == null)
                    {
                        var pendingGrantKey = Guid.NewGuid();
                        var ptPendingGrantLock = transaction.Instrumentation?.CreateToken(InstrumentationTracker.PerformanceCounter.PendingGrantLock, "Write");
                        _pendingGrants.DeadlockAvoidanceTryWrite(10, _core.CancellationToken, (pendingGrants) =>
                        {
                            ptPendingGrantLock?.StopAndAccumulate();
                            pendingGrants.Add(pendingGrantKey, new(transaction, intention));
                        });
                        request.PendingGrantKey = pendingGrantKey;
                    }

                    DetectDeadlock(transaction, intention); //Throws if this transaction is part of a deadlock.

                    result = AttemptResult.Blocked;
                    return null;
                });

                return isLockHeld ? result : AttemptResult.Busy;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{transaction.ProcessId}], object: [{intention.ToString()}].", ex);
                throw;
            }
        }

        /// <summary>
        /// Must be called while holding the lock management critical section.
        /// Throws a KbDeadlockException (and marks the transaction as deadlocked) if the transaction is part of a waits-for cycle.
        /// </summary>
        private void DetectDeadlock(Transaction transaction, ObjectLockIntention intention)
        {
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
            explanation.AppendLine($"            Object: {intention.TargetKey.Canonical}");
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
