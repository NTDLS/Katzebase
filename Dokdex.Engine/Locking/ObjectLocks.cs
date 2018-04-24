using Dokdex.Engine.Exceptions;
using Dokdex.Engine.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using static Dokdex.Engine.Constants;

namespace Dokdex.Engine.Locking
{
    public class ObjectLocks
    {
        private List<ObjectLock> objectLocks = new List<ObjectLock>();
        private Dictionary<Transaction, LockIntention> lockWaits = new Dictionary<Transaction, LockIntention>();

        private Core core;
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
                    objectLocks.Remove(objectLock);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to remove lock.", ex);
                throw;
            }
        }

        public void Acquire(Transaction transaction, LockIntention intention)
        {
            try
            {
                //We keep track of all transactions that are waiting on locks for a few reasons:
                // (1) When we suspect a deadlock we know what all transactions are potentially involved
                // (2) We are safe to poke around those transaction's properties because we know their threda are in this method.
                lock (lockWaits)
                {
                    lockWaits.Add(transaction, intention);
                }

                DateTime startTime = DateTime.UtcNow;

                while (true)
                {
                    lock (CriticalSections.AcquireLock)
                    {
                        if (transaction.IsDeadlocked)
                        {
                            core.Health.Increment(HealthCounterType.DeadlockCount);
                            throw new DokdexDeadlockException(String.Format("Deadlock occurred, transaction for process {0} is being terminated.", transaction.ProcessId));
                        }

                        //Find any existing locks:
                        List<ObjectLock> lockedObjects = new List<ObjectLock>();

                        if (intention.Type == LockType.File)
                        {
                            lockedObjects.AddRange((from o in objectLocks where o.LockType == LockType.File && o.DiskPath == intention.DiskPath select o).ToList());
                        }
                        else
                        {
                            lockedObjects.AddRange((from o in objectLocks where o.DiskPath.StartsWith(intention.DiskPath) select o).ToList());
                        }

                        lockedObjects.AddRange((from o in objectLocks where o.LockType == LockType.Directory && intention.DiskPath.StartsWith(o.DiskPath) select o).ToList());

                        if (lockedObjects.Count() == 0)
                        {
                            //No locks on the object exist - so add one to the local and class collections.
                            var lockedObject = new ObjectLock(core, intention);
                            this.objectLocks.Add(lockedObject);
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
                                    var lockKey = lockedObject.IssueSingleUseKey(transaction, intention);
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
                            //If there are no existing un-owned locks.
                            if (blockers.Count() == 0)
                            {
                                transaction.BlockedBy.Clear();

                                foreach (var lockedObject in lockedObjects)
                                {
                                    var lockKey = lockedObject.IssueSingleUseKey(transaction, intention);
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
                    lock (lockWaits)
                    {
                        var actualWaiters = lockWaits.Keys.Where(o => o.IsDeadlocked == false).ToList();
                        if (actualWaiters.Count() > 1)
                        {
                            foreach (var waiter in actualWaiters)
                            {
                                var blockedByMe = (from o in actualWaiters where o != waiter && o.BlockedBy.Contains(waiter.ProcessId) select o).ToList();

                                if (blockedByMe != null && blockedByMe.Count > 0)
                                {
                                    var blockingMe = (from o in blockedByMe where waiter.BlockedBy.Contains(o.ProcessId) select o).ToList();
                                    if (blockingMe != null && blockingMe.Count > 0)
                                    {
                                        transaction.IsDeadlocked = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }


                    System.Threading.Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write(String.Format("Failed to acquire lock for process {0}.", transaction.ProcessId), ex);
                throw;
            }
            finally
            {
                lock (lockWaits)
                {
                    lockWaits.Remove(transaction);
                }
            }
        }
    }
}
