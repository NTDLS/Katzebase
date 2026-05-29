using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.PersistentTypes.Atomicity
{
    /// <summary>
    /// Snapshot class for Atom, used to snapshot the state of the associated class.
    /// </summary>
    public class AtomSnapshot
    {
        public ActionType Action { get; set; }
        public CacheKey CacheKey { get; set; } = CacheKey.Empty();
        public long Sequence { get; set; } = 0;

        public string? RdbPath { get; set; }
        public KbColumnFamilyName ColumnFamily { get; set; }
        public byte[]? RdbKey { get; set; }
        public byte[]? OriginalData { get; set; }
    }
}
