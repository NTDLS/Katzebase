using Katzebase.PublicLibrary;
using System.Diagnostics;

namespace Katzebase.Engine.Caching
{
    public class CacheManager
    {
        private Dictionary<string, CacheItem> collection = new();
        private readonly Core core;

        public CacheManager(Core core)
        {
            this.core = core;
        }

        public void Remove(string key)
        {
            lock (collection)
            {
                collection.Remove(key);
            }
        }

        public int RemoveStartsWith(string key)
        {
            int removedCount = 0;

            lock (collection)
            {
                foreach (var item in collection)
                {
                    if (item.Key.StartsWith(key))
                    {
                        collection.Remove(key);
                        removedCount++;
                    }
                }
            }

            return removedCount;
        }

        object freeMemoryLock = new object();

        public CacheItem Upsert(string key, object value)
        {
            long tryFreetreshold = core.settings.MaxCacheMemory * 1024 * 1024;
            long currentVirtualMemorySize = Process.GetCurrentProcess().PrivateMemorySize64;

            if (currentVirtualMemorySize > tryFreetreshold)
            {
                lock (freeMemoryLock)
                {
                    bool allowDeferredIOCommit = false;
                    int scavengeIterations = 0;

                    core.Log.Trace("Cache scavenge start: " + (Process.GetCurrentProcess().PrivateMemorySize64 / 1024.0 / 1024.0).ToString("N") + "MB");

                    var triedKeys = new HashSet<string>();

                    while (Process.GetCurrentProcess().WorkingSet64 > (tryFreetreshold * ((100 - core.settings.CacheScavengeBuffer) / 100.0)))
                    {
                        int countOfItemsFreed = 0;

                        lock (collection)
                        {
                            int countOfOldRecordsToEval = (collection.Count / core.settings.CacheScavengeRate);

                            var top10PctOldest = (((from o in collection
                                                    where triedKeys.Contains(o.Key) == false
                                                    select o).ToList())
                                                .OrderByDescending(o => o.Value.LastHit)
                                                .Take(countOfOldRecordsToEval)).ToList();

                            int countOfLeastUsedToEval = (top10PctOldest.Count / core.settings.CacheScavengeRate);

                            var top10PctLeastUsed = (((from o in top10PctOldest
                                                       where triedKeys.Contains(o.Key) == false
                                                       select o).ToList())
                                                .OrderByDescending(o => o.Value.Hits)
                                                .Take(countOfLeastUsedToEval)).ToList();

                            foreach (var itemToRemove in top10PctLeastUsed)
                            {
                                triedKeys.Add(itemToRemove.Key);

                                bool remove = true;

                                foreach (var tx in core.Transactions.Collection)
                                {
                                    Utility.EnsureNotNull(tx.DeferredIOs);

                                    foreach (var deferredIo in tx.DeferredIOs.Collection)
                                    {
                                        if (deferredIo.Key == itemToRemove.Key)
                                        {
                                            if (allowDeferredIOCommit)
                                            {
                                                core.Log.Trace("Comitting " + tx.DeferredIOs.Collection.Count + " deferred IOs for process: " + tx.ProcessId);
                                                tx.DeferredIOs.CommitDeferredDiskIO();
                                            }
                                            else
                                            {
                                                remove = false;
                                            }
                                            break;
                                        }
                                    }
                                }

                                if (remove)
                                {
                                    collection.Remove(itemToRemove.Key);
                                    countOfItemsFreed++;
                                }
                            }

                            core.Log.Trace("Ejected " + countOfItemsFreed.ToString("N") + " cache items.");

                            if (countOfItemsFreed == 0)
                            {
                                core.Log.Trace("Restarting scavenge with allowance for deferred commits.");
                                allowDeferredIOCommit = true;
                            }

                            if (collection.Count == 0)
                            {
                                break;
                            }
                            scavengeIterations++;
                        }

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    core.Log.Trace("Cache scavenge complete: "
                        + (Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0).ToString("N")
                        + "MB (" + scavengeIterations + " iterations).");
                }
            }

            lock (collection)
            {
                if (collection.ContainsKey(key))
                {
                    var cacheItem = collection[key];
                    cacheItem.Updates++;
                    cacheItem.Updated = DateTime.UtcNow;
                    cacheItem.Value = value;
                    return cacheItem;
                }
                else
                {
                    CacheItem cacheItem = new CacheItem()
                    {
                        Hits = 0,
                        Updates = 0,
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow,
                        LastHit = DateTime.UtcNow,
                        Value = value
                    };
                    collection.Add(key, cacheItem);
                    return cacheItem;
                }
            }
        }

        public CacheItem? Get(string key)
        {
            lock (collection)
            {
                if (collection.ContainsKey(key))
                {
                    var cacheItem = collection[key];
                    if (cacheItem != null)
                    {
                        cacheItem.Hits++;
                        cacheItem.LastHit = DateTime.UtcNow;
                        return cacheItem;
                    }
                }
                return null;
            }
        }
    }
}
