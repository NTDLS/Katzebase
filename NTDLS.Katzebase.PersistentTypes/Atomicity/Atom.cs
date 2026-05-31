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
        public CacheKey? CacheKey { get; set; }
        public long Sequence { get; set; } = 0;

        public string RdbPath { get; set; } = string.Empty;
        public string ColumnFamilyName { get; set; } = string.Empty;
        public byte[]? RdbKey { get; set; }
        public byte[]? OriginalData { get; set; }

        public Atom()
        {

        }

        public Atom(ActionType action, long sequence, string rdbPath, string columnFamilyName)
        {
            Action = action;
            Sequence = sequence;
            RdbPath = rdbPath;
            ColumnFamilyName = columnFamilyName;
        }

        public Atom(ActionType action, long sequence, string rdbPath, string columnFamilyName, byte[] rdbKey, CacheKey cacheKey)
        {
            Action = action;
            Sequence = sequence;
            RdbKey = rdbKey;
            CacheKey = cacheKey;
            RdbPath = rdbPath;
            ColumnFamilyName = columnFamilyName;
        }

        public Atom(ActionType action, long sequence, string rdbPath, string columnFamilyName, byte[] rdbKey, CacheKey cacheKey, byte[]? originalData)
        {
            Action = action;
            Sequence = sequence;
            RdbKey = rdbKey;
            CacheKey = cacheKey;
            OriginalData = originalData;
            RdbPath = rdbPath;
            ColumnFamilyName = columnFamilyName;
        }

        public AtomSnapshot Snapshot()
        {
            var snapshot = new AtomSnapshot()
            {
                Action = Action,
                CacheKey = CacheKey,
                Sequence = Sequence,
                RdbPath = RdbPath,
                ColumnFamilyName = ColumnFamilyName,
                RdbKey = RdbKey,
                OriginalData = OriginalData,
            };

            return snapshot;
        }
    }
}
