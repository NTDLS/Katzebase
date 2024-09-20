using NTDLS.Katzebase.Client.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace NTDLS.Katzebase.Engine.Library
{
    public static class Helpers
    {
        public static string ReplaceFirst(this string input, string search, string replacement)
        {
            int pos = input.IndexOf(search);
            if (pos < 0)
            {
                return input; // Return the original string if the search string is not found
            }
            return input.Substring(0, pos) + replacement + input.Substring(pos + search.Length);
        }

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
        public static Dictionary<K, V> IntersectWith<K, V>(this Dictionary<K, V>? one, Dictionary<K, V> two) where K : notnull
        {
            //return one.Where(o => two.ContainsKey(o.Key)).ToDictionary(o => o.Key, o => o.Value);

            if (one == null)
            {
                return two.ToDictionary(o => o.Key, o => o.Value);
            }

            Dictionary<K, V> commonEntries = new();

            foreach (var kvp in one)
            {
                if (two.ContainsKey(kvp.Key))
                {
                    commonEntries[kvp.Key] = kvp.Value;
                }
            }

            return commonEntries;
        }

        public static void CopyDirectory(string sourcePath, string destinationPath)
        {
            Directory.CreateDirectory(destinationPath);

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
                throw new KbNullException($"Value should not be null {nameof(diskPath)}.");
            }

            if (IsDirectoryEmpty(diskPath))
            {
                Directory.Delete(diskPath);
            }
        }

        public static string ComputeSHA256(string rawData)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));

            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
