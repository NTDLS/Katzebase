using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.PersistentTypes.Atomicity
{
    /// <summary>
    /// The atom is a unit of reversable work.
    /// </summary>
    public class Atom
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionType Action { get; set; }
        public CacheKey CacheKey { get; set; }
        public long Sequence { get; set; } = 0;

        public string RdbPath { get; set; }
        public KbColumnFamilyName ColumnFamilyName { get; set; }
        public byte[] RdbKey { get; set; }
        public byte[]? OriginalData { get; set; }

        public Atom(ActionType action, long sequence, string rdbPath, KbColumnFamilyName columnFamily, byte[] rdbKey, CacheKey cacheKey)
        {
            Action = action;
            Sequence = sequence;
            RdbKey = rdbKey;
            CacheKey = cacheKey;
            RdbPath = rdbPath;
            ColumnFamilyName = columnFamily;
        }

        public AtomSnapshot Snapshot()
        {
            var snapshot = new AtomSnapshot()
            {
                Action = Action,
                CacheKey = CacheKey,
                Sequence = Sequence,
                RdbPath = RdbPath,
                ColumnFamily = ColumnFamilyName,
                RdbKey = RdbKey,
                OriginalData = OriginalData,
            };

            return snapshot;
        }
    }
}
