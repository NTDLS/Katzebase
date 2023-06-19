using Katzebase.Engine.Atomicity;
using Katzebase.Engine.KbLib;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Locking
{
    internal class ObjectLocks
    {
        private readonly List<ObjectLock> collection = new();
        private readonly Dictionary<Transaction, LockIntention> transactionWaitingForLocks = new();

        private readonly Core core;
        public ObjectLocks(Core core)
        {
            this.core = core;
        }

        public void Remove(ObjectLock objectLock)
        {
            try
            {
                lock (CriticalSections.AcquireLock)
                {
                    collection.Remove(objectLock);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to remove lock.", ex);
                throw;
            }
        }

        public List<ObjectLock> GloballyHeldLocks(string diskPath, LockType lockType)
        {
            lock (CriticalSections.AcquireLock)
            {
                var lockedObjects = new List<ObjectLock>();

                if (lockType == LockType.File)
                {
                    //See if there are any other locks on this file:
                    lockedObjects.AddRange(collection.Where(o => o.LockType == LockType.File && o.DiskPath == diskPath));
                }

                //See if there are any other locks on the directory containig this file (or directory) or any of its parents.
                //When we lock a directory, we intend all lower directories to be locked too.
                lockedObjects.AddRange(collection.Where(o => o.LockType == LockType.Directory && diskPath.StartsWith(o.DiskPath)));

                return lockedObjects;
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
                lock (transactionWaitingForLocks)
                {
                    transactionWaitingForLocks.Add(transaction, intention);
                }

                DateTime startTime = DateTime.UtcNow;

                while (true)
                {
                    lock (CriticalSections.AcquireLock)
                    {
                        if (transaction.IsDeadlocked)
                        {
                            core.Health.Increment(HealthCounterType.DeadlockCount);
                            throw new KbDeadlockException($"Deadlock occurred, transaction for process {transaction.ProcessId} is being terminated.");
                        }

                        var lockedObjects = GloballyHeldLocks(intention.DiskPath, intention.LockType); //Find any existing locks:

                        if (lockedObjects.Count == 0)
                        {
                            //No locks on the object exist - so add one to the local and class collections.
                            var lockedObject = new ObjectLock(core, intention);
                            collection.Add(lockedObject);
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
                                    Utility.EnsureNotNull(transaction.HeldLockKeys);
                                    transaction.HeldLockKeys.Add(lockKey);
                                }

                                var lockWaitTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                                core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

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
                                    Utility.EnsureNotNull(transaction.HeldLockKeys);
                                    transaction.HeldLockKeys.Add(lockKey);
                                }

                                var lockWaitTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                                core.Health.Increment(HealthCounterType.LockWaitMs, lockWaitTime);
                                core.Health.Increment(HealthCounterType.LockWaitMs, intention.ObjectName, lockWaitTime);

                                return;
                            }
                            else
                            {
                                transaction.BlockedBy.Clear();
                                transaction.BlockedBy.AddRange(blockers.Select(o => o.ProcessId).Distinct());
                            }
                        }
                    }

                    //Deadlock Search.
                    lock (transactionWaitingForLocks)
                    {
                        var actualWaiters = transactionWaitingForLocks.Keys.Where(o => o.IsDeadlocked == false).ToList();
                        if (actualWaiters.Count > 1) //Cant deadlock if there is only 1 transaction.
                        {
                            foreach (var waiter in actualWaiters)
                            {
                                var blockedByMe = (actualWaiters.Where(o => o != waiter && o.BlockedBy.Contains(waiter.ProcessId)));

                                if (blockedByMe.Any())
                                {
                                    var blockingMe = (blockedByMe.Where(o => waiter.BlockedBy.Contains(o.ProcessId)));
                                    if (blockingMe.Any())
                                    {
                                        transaction.IsDeadlocked = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to acquire lock for process {transaction.ProcessId}.", ex);
                throw;
            }
            finally
            {
                lock (transactionWaitingForLocks)
                {
                    transactionWaitingForLocks.Remove(transaction);
                }
            }
        }
    }
}
