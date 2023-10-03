﻿using NTDLS.Katzebase.Engine.Caching;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to cache.
    /// </summary>
    internal class CacheManager
    {
        private readonly Core _core;
        private readonly KbMemoryCache[] _partitions;

        public int PartitionCount { get; private set; }

        public CacheManager(Core core)
        {
            _core = core;

            try
            {
                PartitionCount = core.Settings.CachePartitions > 0 ? core.Settings.CachePartitions : Environment.ProcessorCount;

                _partitions = new KbMemoryCache[PartitionCount];

                int maxMemoryPerPartition = (int)(core.Settings.CacheMaxMemory / (double)PartitionCount);
                maxMemoryPerPartition = maxMemoryPerPartition < 5 ? 5 : maxMemoryPerPartition;

                for (int i = 0; i < PartitionCount; i++)
                {
                    _partitions[i] = new KbMemoryCache(core);
                }
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate cache manager.", ex);
                throw;
            }
        }

        public void Close()
        {
            for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
            {
                _partitions[partitionIndex].Dispose();
            }
        }

        public void Upsert(string key, object value, int aproximateSizeInBytes = 0)
        {
            try
            {
                key = key.ToLower();
                int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);
                _partitions[partitionIndex].Upsert(key, value, aproximateSizeInBytes);
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to upsert cache object.", ex);
                throw;
            }
        }

        public void Clear()
        {
            try
            {
                for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
                {
                    _partitions[partitionIndex].Clear();
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to clear cache.", ex);
                throw;
            }
        }

        public CachePartitionAllocationStats GetPartitionAllocationStatistics()
        {
            try
            {
                var result = new CachePartitionAllocationStats
                {
                    PartitionCount = PartitionCount
                };

                for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
                {
                    lock (_partitions[partitionIndex])
                    {
                        result.Partitions.Add(new CachePartitionAllocationStats.CachePartitionAllocationStat
                        {
                            Partition = partitionIndex,
                            Allocations = _partitions[partitionIndex].Count(),
                            SizeInKilobytes = _partitions[partitionIndex].SizeInKilobytes(),
                            MaxSizeInKilobytes = _partitions[partitionIndex].MaxSizeInKilobytes()
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to clear cache.", ex);
                throw;
            }
        }

        public CachePartitionAllocationDetails GetPartitionAllocationDetails()
        {
            try
            {
                var result = new CachePartitionAllocationDetails
                {
                    PartitionCount = PartitionCount
                };

                for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
                {
                    lock (_partitions[partitionIndex])
                    {
                        foreach (var item in _partitions[partitionIndex].Collection)
                        {
                            result.Partitions.Add(new CachePartitionAllocationDetails.CachePartitionAllocationDetail(item.Key)
                            {
                                Partition = partitionIndex,
                                AproximateSizeInBytes = item.Value.AproximateSizeInBytes,
                                GetCount = item.Value.GetCount,
                                SetCount = item.Value.SetCount,
                                Created = item.Value.Created,
                                LastSetDate = item.Value.LastSetDate,
                                LastGetDate = item.Value.LastGetDate,
                            });
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to clear cache.", ex);
                throw;
            }
        }

        public object? TryGet(string key)
        {
            try
            {
                key = key.ToLower();
                int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

                lock (_partitions[partitionIndex])
                {
                    return _partitions[partitionIndex].TryGet(key);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to get cache object.", ex);
                throw;
            }
        }

        public object Get(string key)
        {
            try
            {
                key = key.ToLower();
                int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

                lock (_partitions[partitionIndex])
                {
                    return _partitions[partitionIndex].Get(key);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to get cache object.", ex);
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

                lock (_partitions[partitionIndex])
                {
                    if (_partitions[partitionIndex].Remove(key))
                    {
                        itemsEjected++;
                    }
                }

                return itemsEjected;
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to remove cache object.", ex);
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
                    lock (_partitions[i])
                    {
                        var keysToRemove = _partitions[i].Keys().Where(entry => entry.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
                        foreach (string key in keysToRemove)
                        {
                            _partitions[i].Remove(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to remove cache prefixed-object.", ex);
                throw;
            }
        }
    }
}