using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    /// <summary>
    /// Snapshot class for ObjectLock, used to snapshot the state of the associated class.
    /// </summary>
    internal class ObjectLockSnapshot
    {
        public string DiskPath { get; set; } = string.Empty;
        public LockGranularity Granularity { get; set; }
        public List<ObjectLockKeySnapshot> Keys { get; set; } = new();
        public ulong Hits { get; set; }
    }
}
