using System.Collections.Specialized;
using System.Runtime.Caching;

namespace Katzebase.Engine.Caching
{
    public class CacheManager
    {
        private readonly int partitionCount = Environment.ProcessorCount;
        private readonly MemoryCache[] partitions;

        public CacheManager(Core core)
        {
            partitions = new MemoryCache[partitionCount];

            var config = new NameValueCollection
            {
                { "CacheMemoryLimitMegabytes", core.settings.MaxCacheMemory.ToString() }
            };

            for (int i = 0; i < partitionCount; i++)
            {
                partitions[i] = new MemoryCache("CacheManager", config);
            }
        }
        public void Upsert(string key, object value)
        {
            int partitionIndex = Math.Abs(key.GetHashCode() % partitionCount);

            var cachePolicy = new CacheItemPolicy()
            {
                SlidingExpiration = TimeSpan.FromHours(1)
            };

            lock (partitions[partitionIndex])
            {
                if (partitions[partitionIndex].Contains(key))
                {
                    partitions[partitionIndex].Set(key, value, cachePolicy);
                }
                else
                {
                    partitions[partitionIndex].Add(key, value, cachePolicy);
                }
            }
        }

        public object? Get(string key)
        {
            int partitionIndex = Math.Abs(key.GetHashCode() % partitionCount);

            lock (partitions[partitionIndex])
            {
                return partitions[partitionIndex].Get(key);
            }
        }

        public void Remove(string key)
        {
            int partitionIndex = Math.Abs(key.GetHashCode() % partitionCount);

            lock (partitions[partitionIndex])
            {
                partitions[partitionIndex].Remove(key);
            }
        }

        public void RemoveItemsWithPrefix(string prefix)
        {
            for (int i = 0; i < partitionCount; i++)
            {
                lock (partitions[i])
                {
                    IEnumerable<string> keysToRemove = partitions[i]
                    .Where(entry => entry.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(entry => entry.Key);

                    foreach (string key in keysToRemove)
                    {
                        partitions[i].Remove(key);
                    }
                }
            }
        }
    }
}
