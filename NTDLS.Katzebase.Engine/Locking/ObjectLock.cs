using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Semaphore;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectLock
    {
        private readonly EngineCore _core;
        public string DiskPath { get; private set; }
        public LockGranularity Granularity { get; private set; }
        public OptimisticCriticalResource<List<ObjectLockKey>> Keys { get; private set; }

        /// <summary>
        /// The total number of times we attempted to lock this object.
        /// If this is a directory lock, then this also includes the number of file locks that deferred to this higher level lock.
        /// </summary>
        public ulong Hits { get; set; }

        public ObjectLock(EngineCore core, ObjectLockIntention intention)
        {
            _core = core;
            Keys = new OptimisticCriticalResource<List<ObjectLockKey>>(core.CriticalSectionLockManagement);
            DiskPath = intention.DiskPath;
            Granularity = intention.Granularity;

            if (Granularity == LockGranularity.Directory && (DiskPath.EndsWith('\\') == false))
            {
                DiskPath = $"{DiskPath}\\";
            }
        }

        public ObjectLockSnapshot Snapshot(bool cloneKeys = false)
        {
            var snapshot = new ObjectLockSnapshot()
            {
                DiskPath = DiskPath,
                Granularity = Granularity,
                Hits = Hits,
            };

            if (cloneKeys) //Prevent stack-overflow.
            {
                Keys.Read((obj) =>
                {
                    foreach (var key in obj)
                    {
                        snapshot.Keys.Add(key.Snapshot());
                    }
                });
            }

            return snapshot;
        }

        public ObjectLockKey IssueSingleUseKey(Transaction transaction, ObjectLockIntention lockIntention)
        {
            try
            {
                var key = new ObjectLockKey(this, transaction.ProcessId, lockIntention.Operation);
                Keys.Write((obj) =>
                {
                    obj.Add(key);
                });
                return key;
            }
            catch (Exception ex)
            {
                Interactions.Management.LogManager.Error("Failed to issue single use lock key.", ex);
                throw;
            }
        }

        public void TurnInKey(ObjectLockKey key)
        {
            try
            {
                Keys.Write((obj) =>
                {
                    obj.Remove(key);

                    if (obj.Count == 0)
                    {
                        _core.Locking.Release(key.ObjectLock);
                    }
                });
            }
            catch (Exception ex)
            {
                Interactions.Management.LogManager.Error("Failed to put turn in lock key.", ex);
                throw;
            }
        }
    }
}
