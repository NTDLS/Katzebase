using System.Buffers.Binary;
using System.Text;

namespace NTDLS.Katzebase.Engine.IO
{
    /// <summary>
    /// Builds and decodes RocksDB keys and values for index entries. There is one entry per indexed document.
    ///
    ///   Values prefix: [field1 UTF-8 lowercase][0x00][field2 UTF-8 lowercase][0x00]...[fieldN UTF-8 lowercase][0x00]
    ///
    ///   Non-unique indexes: key = [values prefix][document id, 4 bytes big-endian], value = empty.
    ///   Unique indexes:     key = [values prefix],                                value = [document id, 4 bytes big-endian].
    ///
    /// Previously there was one entry per distinct value whose value was the packed list of every matching document id.
    /// Every insert or delete had to read and rewrite that whole list (and log its original value for rollback), so the
    /// cost of maintaining a low-cardinality index grew with the size of the table. With one entry per document, inserts
    /// and deletes are a single small write regardless of how many documents share the value. Unique indexes have at most
    /// one document per value, so their key is the values alone, which makes lookups and uniqueness checks a single Get.
    ///
    /// The index identity is carried by the column family name (the index GUID), not the key. The 0x00 separator is
    /// safe because field values are lowercased UTF-8 strings, which never contain 0x00 bytes. Because every key starts
    /// with all of the index's fields, [fields of the leading attributes][0x00] is a prefix shared by exactly the entries
    /// whose leading attributes have those values, which is what lookups seek to.
    /// </summary>
    internal static class IndexKeyBuilder
    {
        private const byte FieldSeparator = 0x00;
        private const int DocumentIdLength = sizeof(uint);
        private static readonly byte[] EmptyValue = [];

        /// <summary>
        /// Builds the key of the index entry for a single document.
        /// </summary>
        public static byte[] BuildEntryKey(bool isUnique, IReadOnlyList<string> fieldValues, uint documentId)
        {
            var prefix = BuildPrefix(fieldValues);
            if (isUnique)
            {
                return prefix;
            }

            var key = new byte[prefix.Length + DocumentIdLength];
            prefix.CopyTo(key, 0);
            BinaryPrimitives.WriteUInt32BigEndian(key.AsSpan(prefix.Length), documentId);
            return key;
        }

        /// <summary>
        /// Builds the value of the index entry for a single document.
        /// </summary>
        public static byte[] BuildEntryValue(bool isUnique, uint documentId)
        {
            if (!isUnique)
            {
                return EmptyValue;
            }

            var value = new byte[DocumentIdLength];
            BinaryPrimitives.WriteUInt32BigEndian(value, documentId);
            return value;
        }

        /// <summary>
        /// Builds the prefix shared by all entries whose leading attributes have the given values:
        /// [field1][0x00]...[fieldN][0x00]. Given values for all of the index's attributes, this is the prefix
        /// of every document indexed with exactly those values (and, for unique indexes, the entire key).
        /// </summary>
        public static byte[] BuildPrefix(IReadOnlyList<string> fieldValues)
        {
            int length = 0;
            foreach (var value in fieldValues)
            {
                length += Encoding.UTF8.GetByteCount(value) + 1;
            }

            var prefix = new byte[length];
            int offset = 0;
            foreach (var value in fieldValues)
            {
                offset += Encoding.UTF8.GetBytes(value, prefix.AsSpan(offset));
                prefix[offset++] = FieldSeparator;
            }
            return prefix;
        }

        /// <summary>
        /// Returns the part of an entry key that identifies its field values (everything except the document id).
        /// Two entries have the same indexed values exactly when these spans are equal.
        /// </summary>
        public static ReadOnlySpan<byte> GetValuePart(bool isUnique, byte[] key)
            => isUnique ? key : key.AsSpan(0, key.Length - DocumentIdLength);

        /// <summary>
        /// Decodes the document id from an index entry.
        /// </summary>
        public static uint DecodeDocumentId(bool isUnique, byte[] key, byte[] value)
            => isUnique
                ? BinaryPrimitives.ReadUInt32BigEndian(value)
                : BinaryPrimitives.ReadUInt32BigEndian(key.AsSpan(key.Length - DocumentIdLength));

        /// <summary>
        /// Decodes the field values from an index entry key.
        /// </summary>
        public static string[] DecodeFieldValues(bool isUnique, byte[] key)
        {
            //Exclude the document id (non-unique only) and the separator that terminates the last field.
            var payload = key.AsSpan(0, key.Length - (isUnique ? 0 : DocumentIdLength) - 1);
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
    }
}
