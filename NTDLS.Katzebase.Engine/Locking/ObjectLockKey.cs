using Newtonsoft.Json;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectLockKey<TData> where TData : IStringable
    {
        public DateTime IssueTime { get; set; }

        [JsonIgnore]
        public ObjectLock<TData> ObjectLock { get; private set; }
        public LockOperation Operation { get; private set; }
        public ulong ProcessId { get; private set; }

        public void TurnInKey()
            => ObjectLock.TurnInKey(this);

        public ObjectLockKey(ObjectLock<TData> objectLock, ulong processId, LockOperation operation)
        {
            IssueTime = DateTime.UtcNow;
            ProcessId = processId;
            ObjectLock = objectLock;
            Operation = operation;
        }

        public string Key => $"{ObjectLock.Granularity}:{Operation}:{ObjectLock.DiskPath}";

        public new string ToString() => Key;

        public ObjectLockKeySnapshot Snapshot() => new ObjectLockKeySnapshot()
        {
            Operation = Operation,
            ProcessId = ProcessId,
            ObjectLock = ObjectLock.Snapshot(false),
            IssueTime = IssueTime,
        };
    }
}
