using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Atomicity
{
    /// <summary>
    /// The atom is a unit of reversable work.
    /// </summary>
    public class Atom
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionType Action { get; set; }
        public string OriginalPath { get; set; }
        public string Key { get; set; }
        public string? BackupPath { get; set; }
        public int Sequence { get; set; } = 0;

        public Atom(ActionType action, string originalPath)
        {
            Action = action;
            OriginalPath = originalPath;
            Key = OriginalPath.ToLowerInvariant();
        }

        public AtomSnapshot Snapshot()
        {
            var snapshot = new AtomSnapshot()
            {
                Action = Action,
                OriginalPath = OriginalPath,
                Key = Key,
                BackupPath = BackupPath,
                Sequence = Sequence
            };

            return snapshot;
        }
    }
}
