namespace NTDLS.Katzebase.Engine.Caching
{
    internal class KbMemoryCache : IDisposable
    {
        internal Dictionary<string, KbCacheItem> Collection { get; private set; } = new();
        private readonly Timer _timer;
        private readonly Core _core;
        private int _cachePartitions;

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

        public KbMemoryCache(Core core)
        {
            _core = core;
            _cachePartitions = _core.Settings.CachePartitions; //We save this because this is NOT changable while the server is running.
            _timer = new Timer(TimerTickCallback, this, TimeSpan.FromSeconds(core.Settings.CacheScavengeInterval), TimeSpan.FromSeconds(core.Settings.CacheScavengeInterval));
        }

        private void TimerTickCallback(object? state)
        {
            var maxMemoryMB = MaxSizeInMegabytes();

            var sizeInMegabytes = SizeInMegabytes();
            if (sizeInMegabytes > maxMemoryMB)
            {
                if (Monitor.TryEnter(this, 50))
                {
                    //When we reach our set memory pressure, we will remove the least recently hit items from cache.
                    //TODO: since we have the hit count, update count, etc. maybe we can make this more intelligent?

                    var oldestGottenItems = Collection.OrderBy(o => o.Value.LastGetDate)
                        .Select(o => new
                        {
                            o.Key,
                            o.Value.AproximateSizeInBytes
                        }
                        ).ToList();

                    double objectSizeSummation = 0;
                    double spaceNeededToClear = (sizeInMegabytes - maxMemoryMB) * 1024.0 * 1024.0;

                    foreach (var item in oldestGottenItems)
                    {
                        Remove(item.Key);
                        objectSizeSummation += item.AproximateSizeInBytes;
                        if (objectSizeSummation >= spaceNeededToClear)
                        {
                            break;
                        }
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

        public double MaxSizeInMegabytes()
        {
            return (_core.Settings.CacheMaxMemory / _cachePartitions);
        }
        public double MaxSizeInKilobytes()
        {
            return (_core.Settings.CacheMaxMemory / _cachePartitions) * 1024.0;
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

        public void Upsert(string key, object obj, int lengthOfUnserializedObject = 0)
        {
            Monitor.Enter(this);
            if (Collection.ContainsKey(key))
            {
                var cacheItem = Collection[key];
                cacheItem.Value = obj;
                cacheItem.SetCount++;
                cacheItem.LastSetDate = DateTime.UtcNow;
                cacheItem.AproximateSizeInBytes = lengthOfUnserializedObject * sizeof(char);

            }
            else
            {
                Collection.Add(key, new KbCacheItem(obj, lengthOfUnserializedObject * sizeof(char)));
            }
            Monitor.Exit(this);
        }
    }
}
