using K4os.Compression.LZ4;
using System.Text;

namespace Katzebase.Engine.Library
{

    public static class Compression
    {
        public static byte[] Compress(byte[] bytes) => LZ4Pickler.Pickle(bytes, LZ4Level.L00_FAST);
        public static byte[] Decompress(byte[] bytes) => LZ4Pickler.Unpickle(bytes);
        public static byte[] Compress(string str) => Compress(Encoding.UTF8.GetBytes(str));
        public static string DecompressString(byte[] bytes) => Encoding.UTF8.GetString(Decompress(bytes));
    }
}
