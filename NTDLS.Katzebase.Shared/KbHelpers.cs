using NTDLS.Katzebase.Api.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace NTDLS.Katzebase.Shared
{
    public static class KbHelpers
    {
        /// <summary>
        /// Adds the values of the given dictionary to the referenced dictionary.
        /// </summary>
        public static void UnionWith<K, V>(this Dictionary<K, V> full, Dictionary<K, V>? partial) where K : notnull
        {
            if (partial != null)
            {
                foreach (var kvp in partial)
                {
                    full[kvp.Key] = kvp.Value;
                }
            }
        }



        /// <summary>
        /// Produces a new dictionary that is the product of the common keys between the two.
        /// If the given dictionary is null, a clone of dictionary two is returned.
        /// </summary>
        public static HashSet<V> MaterializedIntersectWith<V>(this HashSet<V>? one, HashSet<V> two)
        {
            if (one == null)
            {
                return new HashSet<V>(two);
            }

            HashSet<V> commonEntries = new();

            foreach (var kvp in one)
            {
                commonEntries.Add(kvp);
            }

            return commonEntries;
        }

        public static void CopyDirectory(string sourcePath, string destinationPath)
        {
            Directory.CreateDirectory(destinationPath);

            if (Path.Exists(sourcePath))
            {
                //Now Create all of the directories
                foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
                }

                //Copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
                }
            }
        }

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
                sum += (ushort)(sum ^ b);
            }
            return sum;
        }

        public static string GetSHA1Hash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = SHA1.HashData(inputBytes);

            var builder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        public static string GetSHA256Hash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = SHA256.HashData(inputBytes);

            var builder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        public static string GetSHA512Hash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = SHA512.HashData(inputBytes);

            var builder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }

            return builder.ToString();
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
                throw new KbNullException($"Value should not be null: [{nameof(diskPath)}].");
            }

            if (IsDirectoryEmpty(diskPath))
            {
                Directory.Delete(diskPath);
            }
        }
    }
}
