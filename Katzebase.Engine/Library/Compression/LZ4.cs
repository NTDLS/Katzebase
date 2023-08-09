using K4os.Compression.LZ4;
using System.Text;

namespace Katzebase.Engine.Library.Compression
{
    public static class LZ4
    {
        public static byte[] Compress(byte[] bytes, LZ4Level level = LZ4Level.L00_FAST) => LZ4Pickler.Pickle(bytes, level);
        public static byte[] Decompress(byte[] bytes) => LZ4Pickler.Unpickle(bytes);
        public static byte[] Compress(string str, LZ4Level level = LZ4Level.L00_FAST) => LZ4Pickler.Pickle(Encoding.UTF8.GetBytes(str), level);
        public static string DecompressToString(byte[] bytes) => Encoding.UTF8.GetString(LZ4Pickler.Unpickle(bytes));
        public static int DecompressedSize(byte[] bytes) => LZ4Pickler.UnpickledSize(bytes);
}
}
