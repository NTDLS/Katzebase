﻿using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Library;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Locking
{
    internal class ObjectLock
    {
        private readonly Core core;
        public string DiskPath { get; private set; }
        public LockType LockType { get; private set; }
        public List<ObjectLockKey> Keys = new();

        /// <summary>
        /// The total number of times we attmepted to lock this object.
        /// If this is a directory lock, then this also includes the number of file locks that defered to this higher level lock.
        /// </summary>
        public ulong Hits { get; set; }

        public ObjectLock(Core core, LockIntention intention)
        {
            this.core = core;
            DiskPath = intention.DiskPath;
            LockType = intention.LockType;

            if (LockType == LockType.Directory && (DiskPath.EndsWith('\\') == false))
            {
                DiskPath = $"{DiskPath}\\";
            }
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
                lock (CentralCriticalSections.AcquireLock)
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
                lock (CentralCriticalSections.AcquireLock)
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