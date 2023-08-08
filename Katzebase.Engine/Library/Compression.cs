using System.IO.Compression;
using System.Text;

namespace Katzebase.Engine.Library
{
    /*
    public static class Compression
    {
        public static byte[] Compress(byte[] bytes, LZ4Level level = LZ4Level.L00_FAST) => LZ4Pickler.Pickle(bytes, level);
        public static byte[] Decompress(byte[] bytes) => LZ4Pickler.Unpickle(bytes);
        public static byte[] Compress(string str, LZ4Level level = LZ4Level.L00_FAST) => LZ4Pickler.Pickle(Encoding.UTF8.GetBytes(str), level);
        public static string DecompressToString(byte[] bytes) => Encoding.UTF8.GetString(LZ4Pickler.Unpickle(bytes));
        public static int DecompressedSize(byte[] bytes) => LZ4Pickler.UnpickledSize(bytes);
    }
    */
    public static class Compression
    {
        public static byte[] Compress(byte[] bytes)
        {
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new DeflateStream(mso, CompressionMode.Compress))
            {
                msi.CopyTo(gs);
            }
            return mso.ToArray();
        }

        public static byte[] Decompress(byte[] bytes)
        {
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new DeflateStream(msi, CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }
            return mso.ToArray();
        }

        public static byte[] Compress(string str) => Compress(Encoding.UTF8.GetBytes(str));
        public static string DecompressToString(byte[] bytes) => Encoding.UTF8.GetString(Decompress(bytes));
    }
}
