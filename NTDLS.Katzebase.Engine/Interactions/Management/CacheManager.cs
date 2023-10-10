using NTDLS.FastMemoryCache;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to cache.
    /// </summary>
    internal class CacheManager
    {
        private readonly EngineCore _core;
        private readonly PartitionedMemoryCache _cache;

        public int PartitionCount { get; private set; }

        public CacheManager(EngineCore core)
        {
            _core = core;

            try
            {
                var config = new PartitionedCacheConfiguration
                {
                    MaxMemoryMegabytes = core.Settings.CacheMaxMemoryMegabytes,
                    IsCaseSensitive = false,
                    PartitionCount = core.Settings.CachePartitions > 0 ? core.Settings.CachePartitions : Environment.ProcessorCount,
                    ScavengeIntervalSeconds = core.Settings.CacheScavengeInterval > 0 ? core.Settings.CacheScavengeInterval : 30
                };

                _cache = new PartitionedMemoryCache(config);
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instantiate cache manager.", ex);
                throw;
            }
        }

        public void Close()
        {
            _cache.Dispose();
        }

        public void Upsert(string key, object value, int aproximateSizeInBytes = 0)
        {
            try
            {
                _cache.Upsert(key, value, aproximateSizeInBytes);
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
                _cache.Clear();
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
                return _cache.GetPartitionAllocationStatistics(); ;
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
                return _cache.GetPartitionAllocationDetails();
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
                return _cache.TryGet(key);
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
                return _cache.Get(key);
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
                return _cache.Remove(key);
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
                _cache.RemoveItemsWithPrefix(prefix);
            }
            catch (Exception ex)
            {
                _core.Log.Write("Failed to remove cache prefixed-object.", ex);
                throw;
            }
        }
    }
}
