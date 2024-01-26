using Newtonsoft.Json;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectLockKey
    {
        public DateTime IssueTime { get; set; }
        [JsonIgnore]
        public ObjectLock ObjectLock { get; private set; }
        public LockOperation Operation { get; private set; }
        public ulong ProcessId { get; private set; }

        public void TurnInKey()
        {
            ObjectLock.TurnInKey(this);
        }

        /// <summary>
        /// Allows the lock-key to be converted to an observation lock. This is used when we need to
        /// temporarily lock an object in a long running transation but do not want to keep the agressive lock.
        /// 
        /// NOTE: Since the lock manager (ObjectLock) housed in the transaction typically caches the acquired
        /// locks, this function should not be called direcelty but should only be called via ObjectLock.ConvertToStability(...).
        /// </summary>
        internal void ConvertToStability()
        {
            Operation = LockOperation.Stability;
        }

        public ObjectLockKey(ObjectLock objectLock, ulong processId, LockOperation operation)
        {
            IssueTime = DateTime.UtcNow;
            ProcessId = processId;
            ObjectLock = objectLock;
            Operation = operation;
        }

        public string Key => $"{ObjectLock.Granularity}+{Operation}->{ObjectLock.DiskPath}";

        public new string ToString() => Key;

        public ObjectLockKeySnapshot Snapshot()
        {
            var snapshot = new ObjectLockKeySnapshot()
            {
                Operation = Operation,
                ProcessId = ProcessId,
                ObjectLock = ObjectLock.Snapshot(false),
                IssueTime = IssueTime,
            };

            return snapshot;
        }
    }
}
