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
        public CriticalResource<List<ObjectLockKey>> Keys { get; private set; }

        /// <summary>
        /// The total number of times we attmepted to lock this object.
        /// If this is a directory lock, then this also includes the number of file locks that defered to this higher level lock.
        /// </summary>
        public ulong Hits { get; set; }

        public ObjectLock(EngineCore core, LockIntention intention)
        {
            _core = core;
            Keys = new CriticalResource<List<ObjectLockKey>>(core.Locking.CriticalSectionLockManagement);
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
                Keys.Use((obj) =>
                {
                    foreach (var key in obj)
                    {
                        snapshot.Keys.Add(key.Snapshot());
                    }
                });
            }

            return snapshot;
        }

        public ObjectLockKey IssueSingleUseKey(Transaction transaction, LockIntention lockIntention)
        {
            try
            {
                var key = new ObjectLockKey(this, transaction.ProcessId, lockIntention.Operation);
                Keys.Use((obj) =>
                {
                    obj.Add(key);
                });
                return key;
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to issue single use lock key.", ex);
                throw;
            }
        }

        public void TurnInKey(ObjectLockKey key)
        {
            try
            {
                Keys.Use((obj) =>
                {
                    obj.Remove(key);

                    if (obj.Count == 0)
                    {
                        _core.Locking.Remove(key.ObjectLock);
                    }
                });
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to put turn in lock key.", ex);
                throw;
            }
        }
    }
}
