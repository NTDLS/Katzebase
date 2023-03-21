using Katzebase.Engine.Transactions;
using System;
using System.Collections.Generic;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Locking
{
    public class ObjectLock
    {
        private Core core;
        public string DiskPath { get; set; }
        public LockType LockType { get; set; }
        public List<ObjectLockKey> Keys = new List<ObjectLockKey>();

        public ObjectLock(Core core, LockIntention intention)
        {
            this.core = core;
            this.DiskPath = intention.DiskPath;
            this.LockType = intention.Type;
        }

        enum LockDisposition
        {
            Acquire,
            Release
        }

        public ObjectLockKey IssueSingleUseKey(Transaction transaction, LockIntention lockIntention)
        {
            try
            {
                lock (CriticalSections.AcquireLock)
                {
                    var key = new ObjectLockKey(core, this, transaction.ProcessId, lockIntention.Operation);
                    Keys.Add(key);

                    return key;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to issue single use lock key.", ex);
                throw;
            }
        }

        public void TurnInKey(ObjectLockKey key)
        {
            try
            {
                lock (CriticalSections.AcquireLock)
                {
                    Keys.Remove(key);

                    if (Keys.Count == 0)
                    {
                        core.Locking.Remove(key.ObjectLock);
                    }
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to put turn in lock key.", ex);
                throw;
            }
        }
    }
}
