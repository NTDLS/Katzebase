using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Atomicity
{
    /// <summary>
    /// Snapshot class for Atom, used to snapshot the state of the associated class.
    /// </summary>
    public class AtomSnapshot
    {
        public ActionType Action { get; set; }
        public string OriginalPath { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string? BackupPath { get; set; }
        public int Sequence { get; set; } = 0;
    }
}
