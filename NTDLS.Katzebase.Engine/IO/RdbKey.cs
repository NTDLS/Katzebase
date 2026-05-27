using System.Text;

namespace NTDLS.Katzebase.Engine.IO
{
    public class RdbKey
    {
        public byte[] Bytes { get; set; }

        public RdbKey(byte[] key) => Bytes = (byte[])key.Clone();
        public RdbKey(string key) => Bytes = Encoding.UTF8.GetBytes(key.ToLowerInvariant());
        public RdbKey(int key) => Bytes = ToBigEndian(BitConverter.GetBytes(key));
        public RdbKey(uint key) => Bytes = ToBigEndian(BitConverter.GetBytes(key));
        public RdbKey(long key) => Bytes = ToBigEndian(BitConverter.GetBytes(key));
        public RdbKey(ulong key) => Bytes = ToBigEndian(BitConverter.GetBytes(key));
        public RdbKey(Guid key) => Bytes = key.ToByteArray();

        public uint ToUint() => ConvertToUint(Bytes);
        public static uint ConvertToUint(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public int ToInt() => ConvertToInt(Bytes);
        public static int ConvertToInt(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public long ToLong() => ConvertToLong(Bytes);
        public static long ConvertToLong(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public ulong ToULong() => ConvertToULong(Bytes);
        public static ulong ConvertToULong(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public Guid ToGuid() => ConvertToGuid(Bytes);
        public static Guid ConvertToGuid(byte[] bytes) => new(bytes);

        private static byte[] ToBigEndian(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        public override string ToString()
        {
            return Convert.ToHexStringLower(Bytes);
        }
    }
}
