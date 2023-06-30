using System.Collections.Specialized;
using System.Runtime.Caching;

namespace Katzebase.Engine.Caching.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to cache.
    /// </summary>
    public class CacheManager
    {
        private readonly Core core;
        public int PartitionCount { get; private set; }
        public int CachedItemCount => partitions.Sum(o => o.Count());

        private readonly CacheItemPolicy cachePolicy;
        private readonly MemoryCache[] partitions;

        public CacheManager(Core core)
        {
            this.core = core;

            try
            {
                PartitionCount = core.Settings.CachePartitions > 0 ? core.Settings.CachePartitions : Environment.ProcessorCount;

                partitions = new MemoryCache[PartitionCount];

                int maxMemoryPerPartition = (int)(core.Settings.CacheMaxMemory / (double)PartitionCount);
                maxMemoryPerPartition = maxMemoryPerPartition < 5 ? 5 : maxMemoryPerPartition;

                cachePolicy = new CacheItemPolicy()
                {
                    SlidingExpiration = TimeSpan.FromSeconds(core.Settings.CacheSeconds)
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
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate cache manager.", ex);
                throw;
            }

        }

        public void Upsert(string key, object value)
        {
            try
            {
                key = key.ToLower();
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
            catch (Exception ex)
            {
                core.Log.Write("Failed to upsert cache object.", ex);
                throw;
            }
        }

        public void Clear()
        {
            try
            {
                for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
                {
                    lock (partitions[partitionIndex])
                    {
                        var cacheKeys = partitions[partitionIndex].Select(x => x.Key);
                        foreach (string cacheKey in cacheKeys)
                        {
                            partitions[partitionIndex].Remove(cacheKey);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to clear cache.", ex);
                throw;
            }
        }

        public class PartitionAllocationDetails
        {
            public int PartitionCount { get; set; }
            public List<int> PartitionAllocations = new List<int>();
        }


        public PartitionAllocationDetails GetAllocations()
        {
            try
            {
                var result = new PartitionAllocationDetails
                {
                     PartitionCount = PartitionCount
                };

                for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
                {
                    lock (partitions[partitionIndex])
                    {
                        result.PartitionAllocations.Add(partitions[partitionIndex].Count());
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to clear cache.", ex);
                throw;
            }
        }

        public object? Get(string key)
        {
            try
            {
                key = key.ToLower();
                int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

                lock (partitions[partitionIndex])
                {
                    return partitions[partitionIndex].Get(key);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to get cache object.", ex);
                throw;
            }
        }

        public int Remove(string key)
        {
            try
            {
                key = key.ToLower();
                int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

                int itemsEjected = 0;

                lock (partitions[partitionIndex])
                {
                    if (partitions[partitionIndex].Remove(key) != null)
                    {
                        itemsEjected++;
                    }
                }

                return itemsEjected;
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to remove cache object.", ex);
                throw;
            }
        }

        public void RemoveItemsWithPrefix(string prefix)
        {
            try
            {
                prefix = prefix.ToLower();
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
            catch (Exception ex)
            {
                core.Log.Write("Failed to remove cache prefixed-object.", ex);
                throw;
            }
        }
    }
}
