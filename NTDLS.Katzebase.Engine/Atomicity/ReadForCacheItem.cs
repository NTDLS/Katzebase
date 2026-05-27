using NTDLS.Katzebase.PersistentTypes.Atomicity;

namespace NTDLS.Katzebase.Engine.Atomicity
{
    public class ReadForCacheItem
    {
        public CacheKey CacheKey { get; set; }
        public byte[] RdbKey { get; set; }

        public ReadForCacheItem(CacheKey cacheKey, byte[] rdbKey)
        {
            CacheKey = cacheKey;
            RdbKey = rdbKey;
        }

        public override bool Equals(object? obj) =>
            obj is ReadForCacheItem other &&
            CacheKey.Equals(other.CacheKey) &&
            RdbKey.SequenceEqual(other.RdbKey);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(CacheKey);
            hash.AddBytes(RdbKey);
            return hash.ToHashCode();
        }
    }
}
