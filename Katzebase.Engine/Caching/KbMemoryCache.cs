namespace Katzebase.Engine.Caching
{
    internal class KbMemoryCache: IDisposable
    {
        internal int MaxMemoryMB { get; private set; }
        internal Dictionary<string, KbCacheItem> Collection { get; private set; } = new();
        private readonly Timer _timer;

        #region IDisposable

        // This flag indicates whether Dispose has been called already
        private bool disposed = false;

        // Implement IDisposable.Dispose method
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Implement the actual cleanup logic in this method
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Collection.Clear();
                    _timer.Dispose();
                }
                disposed = true;
            }
        }

        #endregion

        public List<string> Keys()
        {
            lock (this)
            {
                return Collection.Select(o => o.Key).ToList();
            }
        }

        public KbMemoryCache(int maxMemoryMB, int pollingInterval)
        {
            MaxMemoryMB = maxMemoryMB;
            _timer = new Timer(TimerTickCallback, this, TimeSpan.FromSeconds(pollingInterval), TimeSpan.FromSeconds(pollingInterval));
        }

        private void TimerTickCallback(object? state)
        {
            var sizeInMegabytes = SizeInMegabytes();
            if (sizeInMegabytes > MaxMemoryMB)
            {
                if (Monitor.TryEnter(this, 50))
                {
                    //When we reach our set memory pressure, we will remove the least recently hit items from cache.
                    //TODO: since we have the hit count, update count, etc. maybe we can make this more intelligent?

                    var oldestGottenItems = Collection.OrderBy(o => o.Value.LastGetDate);
                    var itemsToRemove = new List<string>();

                    double objectSizeSummation = 0;
                    double spaceNeededToClear = (sizeInMegabytes - MaxMemoryMB) * 1024.0 * 1024.0;

                    foreach (var item in oldestGottenItems)
                    {
                        itemsToRemove.Add(item.Key);

                        objectSizeSummation += item.Value.AproximateSizeInBytes;
                        if (objectSizeSummation >= spaceNeededToClear)
                        {
                            break;
                        }
                    }

                    foreach (var item in itemsToRemove)
                    {
                        Remove(item);
                    }

                    Monitor.Exit(this);
                }
            }
        }

        public double SizeInMegabytes()
        {
            Monitor.Enter(this);
            var result = Collection.Sum(o => o.Value.AproximateSizeInBytes / 1024.0 / 1024.0);
            Monitor.Exit(this);
            return result;
        }

        public double SizeInKilobytes()
        {
            Monitor.Enter(this);
            var result = Collection.Sum(o => o.Value.AproximateSizeInBytes / 1024.0);
            Monitor.Exit(this);
            return result;

        }

        public int Count()
        {
            Monitor.Enter(this);
            var result = Collection.Count;
            Monitor.Exit(this);
            return result;
        }

        public bool Contains(string key)
        {
            Monitor.Enter(this);
            var result = Collection.ContainsKey(key);
            Monitor.Exit(this);
            return result;
        }

        public object Get(string key)
        {
            Monitor.Enter(this);
            var result = Collection[key];
            result.GetCount++;
            result.LastGetDate = DateTime.UtcNow;
            Monitor.Exit(this);
            return result.Value;
        }

        public bool Remove(string key)
        {
            Monitor.Enter(this);
            var result = Collection.Remove(key);
            Monitor.Exit(this);
            return result;
        }

        public void Clear()
        {
            Monitor.Enter(this);
            Collection.Clear();
            Monitor.Exit(this);
        }

        public object? TryGet(string key)
        {
            Monitor.Enter(this);
            KbCacheItem? result = null;
            if (Collection.ContainsKey(key))
            {
                result = Collection[key];
                result.GetCount++;
                result.LastGetDate = DateTime.UtcNow;
            }
            Monitor.Exit(this);
            return result?.Value;
        }

        public bool TryGetValue(string key, out object? value)
        {
            Monitor.Enter(this);
            if (Collection.TryGetValue(key, out var result))
            {
                Monitor.Exit(this);
                result.GetCount++;
                result.LastGetDate = DateTime.UtcNow;
                value = result.Value;
                return true;
            }
            else
            {
                Monitor.Exit(this);
                value = null;
                return false;
            }
        }

        public void Upsert(string key, object obj, int aproximateSizeInBytes = 0)
        {
            Monitor.Enter(this);
            if (Collection.ContainsKey(key))
            {
                var cacheItem = Collection[key];
                cacheItem.Value = obj;
                cacheItem.SetCount++;
                cacheItem.LastSetDate = DateTime.UtcNow;
                cacheItem.AproximateSizeInBytes = aproximateSizeInBytes;
            }
            else
            {
                Collection.Add(key, new KbCacheItem(obj, aproximateSizeInBytes));
            }
            Monitor.Exit(this);
        }
    }
}
