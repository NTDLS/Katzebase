using Newtonsoft.Json;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectLockKey(ObjectLock objectLock, ulong processId, LockOperation operation)
    {
        public DateTime IssueTime { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public ObjectLock ObjectLock { get; private set; } = objectLock;
        public LockOperation Operation { get; private set; } = operation;
        public ulong ProcessId { get; private set; } = processId;

        public void TurnInKey()
            => ObjectLock.TurnInKey(this);

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
