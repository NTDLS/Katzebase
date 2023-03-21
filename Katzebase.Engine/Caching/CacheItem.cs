using System;

namespace Katzebase.Engine.Caching
{
    public class CacheItem
    {
        public object Value { get; set; }
        public ulong Hits { get; set; }
        public ulong Updates { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public DateTime LastHit { get; set; }
    }
}
