using K4os.Compression.LZ4;
using System.Runtime.CompilerServices;
using System.Text;

namespace Katzebase.Engine.Library
{
    public static class Compression
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Compress(byte[] bytes, LZ4Level level = LZ4Level.L00_FAST) => LZ4Pickler.Pickle(bytes, level);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Decompress(byte[] bytes) => LZ4Pickler.Unpickle(bytes);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Compress(string str, LZ4Level level = LZ4Level.L00_FAST) => LZ4Pickler.Pickle(Encoding.UTF8.GetBytes(str), level);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string DecompressToString(byte[] bytes) => Encoding.UTF8.GetString(LZ4Pickler.Unpickle(bytes));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DecompressedSize(byte[] bytes) => LZ4Pickler.UnpickledSize(bytes);
    }
}
