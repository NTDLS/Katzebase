using System.Collections.Specialized;
using System.Runtime.Caching;

namespace Katzebase.Engine.Caching
{
    public class CacheManager
    {
        public int PartitionCount { get; private set; }
        public int CachedItemCount => partitions.Sum(o => o.Count());

        private CacheItemPolicy cachePolicy;
        private readonly MemoryCache[] partitions;

        public CacheManager(Core core)
        {
            PartitionCount = core.settings.CachePartitions > 0 ? core.settings.CachePartitions : Environment.ProcessorCount;

            partitions = new MemoryCache[PartitionCount];

            int maxMemoryPerPartition = (int)((double)core.settings.CacheMaxMemory / (double)PartitionCount);
            maxMemoryPerPartition = maxMemoryPerPartition < 5 ? 5 : maxMemoryPerPartition;

            cachePolicy = new CacheItemPolicy()
            {
                SlidingExpiration = TimeSpan.FromSeconds(core.settings.CacheSeconds)
            };

            var config = new NameValueCollection
            {
                { "CacheMemoryLimitMegabytes", $"{maxMemoryPerPartition}" }
            };

            for (int i = 0; i < PartitionCount; i++)
            {
                partitions[i] = new MemoryCache("CacheManager", config);
            }
        }

        public void Upsert(string key, object value)
        {
            int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

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
            int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

            lock (partitions[partitionIndex])
            {
                return partitions[partitionIndex].Get(key);
            }
        }

        public void Remove(string key)
        {
            int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

            lock (partitions[partitionIndex])
            {
                partitions[partitionIndex].Remove(key);
            }
        }

        public void RemoveItemsWithPrefix(string prefix)
        {
            for (int i = 0; i < PartitionCount; i++)
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
