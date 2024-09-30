using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    /// <summary>
    /// Snapshot class for ObjectLockKey, used to snapshot the state of the associated class.
    /// </summary>
    internal class ObjectLockKeySnapshot
    {
        public DateTime IssueTime { get; set; }
        public ObjectLockSnapshot ObjectLock { get; set; } = new();
        public LockOperation Operation { get; set; }
        public ulong ProcessId { get; set; }

        public string ObjectName
            => $"{ObjectLock.Granularity}:{ObjectLock.DiskPath}";

        public new string ToString()
            => $"{ObjectLock.Granularity}:{Operation}:{ObjectLock.DiskPath}";
    }
}
