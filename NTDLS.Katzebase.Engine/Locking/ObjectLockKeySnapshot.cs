using static NTDLS.Katzebase.Engine.Library.EngineConstants;

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

        public new string ToString()
        {
            return $"{ObjectLock.Granularity}+{Operation}->{ObjectLock.DiskPath}";
        }
    }
}
