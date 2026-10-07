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

        #region Binary serialization.

        /// <summary>
        /// Version of the binary format written by ToBytes(), stored as the first byte.
        /// </summary>
        private const byte BinaryFormatVersion = 1;

        /// <summary>
        /// Serializes the atom into a compact binary form for the transaction log. One atom is written for every
        /// key a transaction creates, alters or deletes, so this is considerably cheaper than JSON (no reflection,
        /// no base64 encoding of keys and original values).
        /// </summary>
        public byte[] ToBytes()
        {
            using var stream = new MemoryStream(64 + (RdbKey?.Length ?? 0) + (OriginalData?.Length ?? 0) + RdbPath.Length * 2);
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(BinaryFormatVersion);
                writer.Write((byte)Action);
                writer.Write(Sequence);
                writer.Write(RdbPath);
                writer.Write(ColumnFamilyName);

                writer.Write(CacheKey != null);
                if (CacheKey != null)
                {
                    writer.Write(CacheKey.FilePath);
                    writer.Write(CacheKey.Canonical);
                }

                WriteBytes(writer, RdbKey);
                WriteBytes(writer, OriginalData);
            }
            return stream.ToArray();

            static void WriteBytes(BinaryWriter writer, byte[]? bytes)
            {
                writer.Write(bytes?.Length ?? -1);
                if (bytes != null)
                {
                    writer.Write(bytes);
                }
            }
        }

        public static Atom FromBytes(byte[] bytes)
        {
            using var reader = new BinaryReader(new MemoryStream(bytes, writable: false), System.Text.Encoding.UTF8);

            var version = reader.ReadByte();
            if (version != BinaryFormatVersion)
            {
                throw new InvalidDataException($"Unsupported transaction atom format version: [{version}].");
            }

            var atom = new Atom
            {
                Action = (ActionType)reader.ReadByte(),
                Sequence = reader.ReadInt64(),
                RdbPath = reader.ReadString(),
                ColumnFamilyName = reader.ReadString()
            };

            if (reader.ReadBoolean())
            {
                var filePath = reader.ReadString();
                var canonical = reader.ReadString();
                atom.CacheKey = new CacheKey(filePath, canonical);
            }

            atom.RdbKey = ReadBytes(reader);
            atom.OriginalData = ReadBytes(reader);

            return atom;

            static byte[]? ReadBytes(BinaryReader reader)
            {
                var length = reader.ReadInt32();
                return length < 0 ? null : reader.ReadBytes(length);
            }
        }

        #endregion

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
