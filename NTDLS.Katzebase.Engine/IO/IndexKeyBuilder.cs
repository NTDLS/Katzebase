using System.Text;

namespace NTDLS.Katzebase.Engine.IO
{
    /// <summary>
    /// Builds and decodes RocksDB keys for index entries.
    ///
    /// Key format: [field1 UTF-8 lowercase][0x00][field2 UTF-8 lowercase]...[fieldN UTF-8 lowercase]
    ///
    /// The index identity is carried by the column family name (the index GUID), not the key,
    /// so no GUID prefix is needed. The 0x00 separator between fields is safe because field
    /// values are lowercased UTF-8 strings, which never contain 0x00 bytes.
    /// </summary>
    internal static class IndexKeyBuilder
    {
        private const byte FieldSeparator = 0x00;

        /// <summary>
        /// Builds the full key for an exact index entry (used for inserts, deletes, and point lookups).
        /// </summary>
        public static byte[] Build(IReadOnlyList<string> fieldValues)
        {
            using var ms = new MemoryStream(fieldValues.Sum(v => v.Length) + fieldValues.Count);
            for (int i = 0; i < fieldValues.Count; i++)
            {
                if (i > 0) ms.WriteByte(FieldSeparator);
                ms.Write(Encoding.UTF8.GetBytes(fieldValues[i]));
            }
            return ms.ToArray();
        }

        /// <summary>
        /// Builds a seek prefix for scanning entries that match the given leading field values.
        /// Appends a trailing 0x00 after equality fields so the seek lands at the first entry
        /// whose next field begins, rather than at or before a key that ends at this prefix.
        ///
        /// Example — index on (FirstName, LastName, Country), seeking FirstName=john:
        ///   seek key: [j][o][h][n][0x00]
        ///   scan while: key.StartsWith(seekPrefix)
        ///
        /// Example — seeking FirstName=john AND LastName starts with park:
        ///   seek key: [j][o][h][n][0x00][p][a][r][k]
        ///   scan while: key.StartsWith(seekPrefix)   (no trailing 0x00 — prefix match on "park")
        /// </summary>
        public static byte[] BuildSeekPrefix(IReadOnlyList<string> equalityFields, string? rangePrefix = null)
        {
            using var ms = new MemoryStream(64);

            for (int i = 0; i < equalityFields.Count; i++)
            {
                if (i > 0) ms.WriteByte(FieldSeparator);
                ms.Write(Encoding.UTF8.GetBytes(equalityFields[i]));
            }

            if (rangePrefix != null)
            {
                ms.WriteByte(FieldSeparator);
                ms.Write(Encoding.UTF8.GetBytes(rangePrefix));
            }
            else if (equalityFields.Count > 0)
            {
                ms.WriteByte(FieldSeparator);
            }

            return ms.ToArray();
        }

        /// <summary>
        /// Decodes the field values from an index key.
        /// </summary>
        public static string[] DecodeFieldValues(byte[] key)
        {
            var payload = key.AsSpan();
            var result = new List<string>();
            int start = 0;
            for (int i = 0; i <= payload.Length; i++)
            {
                if (i == payload.Length || payload[i] == FieldSeparator)
                {
                    result.Add(Encoding.UTF8.GetString(payload.Slice(start, i - start)));
                    start = i + 1;
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Packs a list of document IDs into a compact byte array (4 bytes each, little-endian).
        /// </summary>
        public static byte[] PackDocumentIds(IReadOnlyList<uint> documentIds)
        {
            var bytes = new byte[documentIds.Count * sizeof(uint)];
            for (int i = 0; i < documentIds.Count; i++)
                BitConverter.TryWriteBytes(bytes.AsSpan(i * sizeof(uint)), documentIds[i]);
            return bytes;
        }

        /// <summary>
        /// Unpacks a byte array into a list of document IDs.
        /// </summary>
        public static List<uint> UnpackDocumentIds(byte[] bytes)
        {
            var result = new List<uint>(bytes.Length / sizeof(uint));
            for (int i = 0; i < bytes.Length; i += sizeof(uint))
                result.Add(BitConverter.ToUInt32(bytes, i));
            return result;
        }
    }
}
