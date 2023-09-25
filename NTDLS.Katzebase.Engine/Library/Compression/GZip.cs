using System.IO.Compression;
using System.Text;

namespace NTDLS.Katzebase.Engine.Library.Compression
{
    public static class GZip
    {
        public static byte[] Compress(byte[] bytes)
        {
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                msi.CopyTo(gs);
            }
            return mso.ToArray();
        }

        public static byte[] Decompress(byte[] bytes)
        {
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }
            return mso.ToArray();
        }

        public static byte[] Compress(string str) => Compress(Encoding.UTF8.GetBytes(str));
        public static string DecompressToString(byte[] bytes) => Encoding.UTF8.GetString(Decompress(bytes));
    }
}
