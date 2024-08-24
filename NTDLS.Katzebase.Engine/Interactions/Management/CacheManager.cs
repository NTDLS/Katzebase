using NTDLS.FastMemoryCache;
using NTDLS.FastMemoryCache.Metrics;
using System.Diagnostics.CodeAnalysis;

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
                    MaxMemoryBytes = core.Settings.CacheMaxMemoryMegabytes * 1024L * 1024L,
                    IsCaseSensitive = false,
                    PartitionCount = core.Settings.CachePartitions > 0 ? core.Settings.CachePartitions : Environment.ProcessorCount,
                    ScavengeIntervalSeconds = core.Settings.CacheScavengeInterval > 0 ? core.Settings.CacheScavengeInterval : 30
                };

                _cache = new PartitionedMemoryCache(config);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate cache manager.", ex);
                throw;
            }
        }

        public void Close()
        {
            _cache.Dispose();
        }

        public void Upsert(string key, object value, int approximateSizeInBytes = 0)
        {
            try
            {
                _cache.Upsert(key, value, approximateSizeInBytes);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to upsert cache object.", ex);
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
                LogManager.Error("Failed to clear cache.", ex);
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
                LogManager.Error("Failed to clear cache.", ex);
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
                LogManager.Error("Failed to clear cache.", ex);
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
                LogManager.Error("Failed to get cache object.", ex);
                throw;
            }
        }

        public bool TryGet(string key, [NotNullWhen(true)] out object? value)
        {
            try
            {
                if (_cache.TryGet(key, out value))
                {
                    return true;
                }
                value = default;
                return false;
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to get cache object.", ex);
                throw;
            }
        }

        public bool TryGet<T>(string key, [NotNullWhen(true)] out T? value)
        {
            try
            {
                if (_cache.TryGet(key, out value))
                {
                    return true;
                }
                value = default;
                return false;
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to get cache object.", ex);
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
                LogManager.Error("Failed to get cache object.", ex);
                throw;
            }
        }

        public bool Remove(string key)
        {
            try
            {
                return _cache.Remove(key);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to remove cache object.", ex);
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
                LogManager.Error("Failed to remove cache prefixed-object.", ex);
                throw;
            }
        }
    }
}
