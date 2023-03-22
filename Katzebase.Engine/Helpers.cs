using System.Text;
using System.Text.RegularExpressions;

namespace Katzebase.Engine
{
    public static class Helpers
    {
        public static Regex RegExRemoveModFileName = new Regex("(\\\\\\{.*\\})");

        public static string RemoveModFileName(string input)
        {
            return RegExRemoveModFileName.Replace(input, "");
        }

        public static void CopyDirectory(string sourcePath, string destinationPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
        }

        public static string GetDocumentModFilePath(Guid id)
        {
            string idString = id.ToString();
            int checksum = Checksum(idString);
            return $"{{{(checksum % 1000)}}}\\{idString}{Constants.DocumentExtension}";
        }

        /*
        public static long EstimateObjectSize(object o)
        {
            using (Stream s = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(s, o);
                return s.Length;
            }
        }
        */

        public static string MakeSafeFileName(string filename)
        {
            Array.ForEach(Path.GetInvalidFileNameChars(),
                  c => filename = filename.Replace(c.ToString(), string.Empty));

            return filename;
        }

        public static ushort Checksum(string buffer)
        {
            return Checksum(Encoding.ASCII.GetBytes(buffer));
        }

        public static ushort Checksum(byte[] buffer)
        {
            ushort sum = 0;
            foreach (var b in buffer)
            {
                sum += (ushort)(sum ^ ((ushort)b));
            }
            return sum;
        }

        public static bool IsDirectoryEmpty(string path)
        {
            if (Directory.Exists(path))
            {
                return !Directory.EnumerateFileSystemEntries(path).Any();
            }
            return false;
        }
        public static void RemoveDirectoryIfEmpty(string? diskPath)
        {
            if (diskPath == null)
            {
                throw new ArgumentNullException(nameof(diskPath));
            }

            if (IsDirectoryEmpty(diskPath))
            {
                Directory.Delete(diskPath);
            }
        }
    }
}
