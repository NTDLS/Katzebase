namespace Katzebase.Engine.Caching
{
    public class CacheItem
    {
        public object? Value { get; set; }
        public ulong Hits { get; set; } = 0;
        public ulong Updates { get; set; } = 0;
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
        public DateTime? LastHit { get; set; }
    }
}
