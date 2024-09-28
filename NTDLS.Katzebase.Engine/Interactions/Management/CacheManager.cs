using NTDLS.FastMemoryCache;
using NTDLS.FastMemoryCache.Metrics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to cache.
    /// </summary>
    internal class CacheManager
    {
        private readonly EngineCore _core;
        private readonly PartitionedMemoryCache _cache;
        private bool _keepRunning = false;

        internal int PartitionCount { get; private set; }

        internal CacheManager(EngineCore core)
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

                _keepRunning = true;

                new Thread(() => CacheMonitorThreadProc()).Start();
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to instantiate cache manager.", ex);
                throw;
            }
        }

        internal void Stop()
        {
            _keepRunning = false;
            _cache.Dispose();
        }

        private void CacheMonitorThreadProc()
        {
            var lastPollTime = DateTime.UtcNow;

            while (_keepRunning)
            {
                if (DateTime.UtcNow - lastPollTime > TimeSpan.FromSeconds(_core.Settings.LargeObjectHeapCompactionInterval))
                {
                    lastPollTime = DateTime.UtcNow;
                    if (_core.Transactions == null)
                    {
                        continue;
                    }
                    var privateMemory = Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024;

                    var areTransactionsActive = _core.Transactions.Snapshot().Count != 0;

                    //If there are no active transactions or we are over a given threshold of memory.
                    if (areTransactionsActive == false || privateMemory > _core.Settings.CacheMaxMemoryMegabytes * 1.25)
                    {
                        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                        if (areTransactionsActive == false)
                        {
                            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
                        }
                        else
                        {
                            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, false);
                        }
                    }
                }
                Thread.Sleep(500);
            }
        }

        internal void Upsert(string key, object value, int approximateSizeInBytes = 0)
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

        internal void Clear()
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

        internal CachePartitionAllocationStats GetPartitionAllocationStatistics()
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

        internal CachePartitionAllocationDetails GetPartitionAllocationDetails()
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

        internal object? TryGet(string key)
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

        internal bool TryGet(string key, [NotNullWhen(true)] out object? value)
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

        internal bool TryGet<T>(string key, [NotNullWhen(true)] out T? value)
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

        internal object Get(string key)
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

        internal bool Remove(string key)
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

        internal void RemoveItemsWithPrefix(string prefix)
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
