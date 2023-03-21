using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Katzebase.Engine.Caching
{
    public class CacheItem
    {
        public object Value { get; set; }
        public UInt64 Hits { get; set; }
        public UInt64 Updates { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public DateTime LastHit { get; set; }
    }
}
