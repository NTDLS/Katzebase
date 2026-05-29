namespace NTDLS.Katzebase.PersistentTypes.Atomicity
{
    public class CacheKey
    {
        /// <summary>
        /// The file path associated with this cache key.
        /// This is used for locking and cache management purposes, and may not be unique across different cache keys.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// The cache key's canonical string representation, used for equality and hashing.
        /// Typically contains the file path and additional information such as column family and key for database entries.
        /// </summary>
        public string Canonical { get; private set; }

        public CacheKey(string filePath, string canonical)
        {
            FilePath = filePath.ToLowerInvariant();
            Canonical = canonical.ToLowerInvariant();
        }

        public CacheKey(string filePath)
        {
            FilePath = filePath.ToLowerInvariant();
            Canonical = FilePath.ToLowerInvariant();
        }

        public static CacheKey Empty()
        {
            return new CacheKey(string.Empty, string.Empty);
        }

        public override string ToString() => Canonical;
        public override int GetHashCode() => Canonical.GetHashCode();

        public override bool Equals(object? obj)
        {
            if (obj is CacheKey other)
            {
                return string.Equals(Canonical, other.Canonical);
            }
            return false;
        }
    }
}
