using NTDLS.FastMemoryCache;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.PersistentTypes.Atomicity;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to cache.
    /// </summary>
    internal class CacheManager
    {
        private readonly EngineCore _core;
        private readonly PartitionedMemoryCache _cache;
        private volatile bool _keepRunning = false;
        private readonly Thread _cacheMonitorThread;

        public static CacheKey MakeCacheKey(string rdbPath, KbColumnFamilyName columnFamily, byte[] key) => MakeCacheKey(rdbPath, columnFamily, new RdbKey(key));
        public static CacheKey MakeCacheKey(string rdbPath, KbColumnFamilyName columnFamily, string key) => MakeCacheKey(rdbPath, columnFamily, new RdbKey(key));
        public static CacheKey MakeCacheKey(string rdbPath, KbColumnFamilyName columnFamily, int key) => MakeCacheKey(rdbPath, columnFamily, new RdbKey(key));
        public static CacheKey MakeCacheKey(string rdbPath, KbColumnFamilyName columnFamily, uint key) => MakeCacheKey(rdbPath, columnFamily, new RdbKey(key));
        public static CacheKey MakeCacheKey(string rdbPath, KbColumnFamilyName columnFamily, long key) => MakeCacheKey(rdbPath, columnFamily, new RdbKey(key));
        public static CacheKey MakeCacheKey(string rdbPath, KbColumnFamilyName columnFamily, ulong key) => MakeCacheKey(rdbPath, columnFamily, new RdbKey(key));
        public static CacheKey MakeCacheKey(string rdbPath, KbColumnFamilyName columnFamily, Guid key) => MakeCacheKey(rdbPath, columnFamily, new RdbKey(key));
        public static CacheKey MakeCacheKey(string rdbPath, KbColumnFamilyName columnFamily, RdbKey key) => new(rdbPath, $"{rdbPath}:{columnFamily}:{key}");
        public static CacheKey MakeCacheKey(string rdbPath, KbColumnFamilyName columnFamily) => new(rdbPath, $"{rdbPath}:{columnFamily}");

        internal int PartitionCount { get; private set; }

        internal CacheManager(EngineCore core)
        {
            _core = core;
            try
            {
                var config = new PartitionedCacheConfiguration
                {
                    SizeLimitBytes = core.Settings.CacheMaxMemoryMegabytes * 1024L * 1024L,
                    IsCaseSensitive = false,
                    PartitionCount = core.Settings.CachePartitions > 0 ? core.Settings.CachePartitions : Environment.ProcessorCount,
                    ExpirationScanFrequency = TimeSpan.FromSeconds(core.Settings.CacheScavengeInterval > 0 ? core.Settings.CacheScavengeInterval : 30)
                };

                _cache = new PartitionedMemoryCache(config);

                _keepRunning = true;

                _cacheMonitorThread = new Thread(() => CacheMonitorThreadProc()) { IsBackground = true };
                _cacheMonitorThread.Start();
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
            _cacheMonitorThread.Join();
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

        internal void Set(CacheKey key, object value, int approximateSizeInBytes = 0)
        {
            try
            {
                var ttl = _core.Settings.CacheSeconds > 0 ? TimeSpan.FromSeconds(_core.Settings.CacheSeconds) : (TimeSpan?)null;
                _cache.Upsert(key.Canonical, value, approximateSizeInBytes, ttl);
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

        internal bool TryGet(CacheKey key, [NotNullWhen(true)] out object? value)
        {
            try
            {
                if (_cache.TryGet(key.Canonical, out value))
                {
                    return value != null;
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

        internal bool TryGet<T>(CacheKey key, [NotNullWhen(true)] out T? value)
        {
            try
            {
                if (_cache.TryGet(key.Canonical, out value))
                {
                    return value != null;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to get cache object.", ex);
                throw;
            }
        }

        internal object? Get(CacheKey key)
        {
            try
            {
                return _cache.Get(key.Canonical);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to get cache object.", ex);
                throw;
            }
        }

        internal void Remove(CacheKey key)
        {
            try
            {
                _cache.Remove(key.Canonical);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to remove cache object.", ex);
                throw;
            }
        }

        internal void RemoveItemsWithPrefix(CacheKey cacheKey)
        {
            try
            {
                _cache.RemoveItemsWithPrefix(cacheKey.Canonical);
            }
            catch (Exception ex)
            {
                LogManager.Error("Failed to remove cache prefixed-object.", ex);
                throw;
            }
        }
    }
}
