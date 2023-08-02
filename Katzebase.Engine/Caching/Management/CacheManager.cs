using System.Runtime.Caching;
using System.Threading;

namespace Katzebase.Engine.Caching.Management
{
   

    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to cache.
    /// </summary>
    public class CacheManager
    {
        private readonly Core core;
        public int PartitionCount { get; private set; }
        private readonly KbMemoryCache[] partitions;

        public CacheManager(Core core)
        {
            this.core = core;

            try
            {
                PartitionCount = core.Settings.CachePartitions > 0 ? core.Settings.CachePartitions : Environment.ProcessorCount;

                partitions = new KbMemoryCache[PartitionCount];

                int maxMemoryPerPartition = (int)(core.Settings.CacheMaxMemory / (double)PartitionCount);
                maxMemoryPerPartition = maxMemoryPerPartition < 5 ? 5 : maxMemoryPerPartition;

                for (int i = 0; i < PartitionCount; i++)
                {
                    partitions[i] = new KbMemoryCache(maxMemoryPerPartition, core.Settings.CacheScavengeInterval);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate cache manager.", ex);
                throw;
            }
        }

        public void Upsert(string key, object value, int aproximateSizeInBytes = 0)
        {
            try
            {
                key = key.ToLower();
                int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);
                partitions[partitionIndex].Upsert(key, value, aproximateSizeInBytes);
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
                    partitions[partitionIndex].Clear();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to clear cache.", ex);
                throw;
            }
        }

        public PartitionAllocationsDetails GetAllocations()
        {
            try
            {
                var result = new PartitionAllocationsDetails
                {
                    PartitionCount = PartitionCount
                };

                for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
                {
                    lock (partitions[partitionIndex])
                    {
                        result.Partitions.Add(new PartitionAllocationsDetails.PartitionAllocationDetails
                        {
                            Allocations = partitions[partitionIndex].Count(),
                            SizeInKilobytes = partitions[partitionIndex].SizeInKilobytes()
                        });
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

        public object? TryGet(string key)
        {
            try
            {
                key = key.ToLower();
                int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

                lock (partitions[partitionIndex])
                {
                    return partitions[partitionIndex].TryGet(key);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to get cache object.", ex);
                throw;
            }
        }

        public object Get(string key)
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
                    if (partitions[partitionIndex].Remove(key))
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
                        var keysToRemove = partitions[i].Keys().Where(entry => entry.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
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
