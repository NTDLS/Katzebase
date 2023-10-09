namespace NTDLS.Katzebase.Engine.Caching
{
    internal class KbMemoryCache : IDisposable
    {
        private readonly Dictionary<string, KbCacheItem> _collection = new();
        private readonly Timer _timer;
        private readonly EngineCore _core;
        private readonly int _cachePartitions;

        #region IDisposable

        // This flag indicates whether Dispose has been called already
        private bool _disposed = false;

        // Implement IDisposable.Dispose method
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Implement the actual cleanup logic in this method
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _collection.Clear();
                    _timer.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion

        public List<string> CloneKeys()
        {
            lock (this)
            {
                return _collection.Select(o => o.Key).ToList();
            }
        }

        public Dictionary<string, KbCacheItem> CloneCollection()
        {
            lock (this)
            {
                return _collection.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Clone()
                );
            }
        }

        public KbMemoryCache(EngineCore core)
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

                    var oldestGottenItems = _collection.OrderBy(o => o.Value.LastGetDate)
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
            var result = _collection.Sum(o => o.Value.AproximateSizeInBytes / 1024.0 / 1024.0);
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
            var result = _collection.Sum(o => o.Value.AproximateSizeInBytes / 1024.0);
            Monitor.Exit(this);
            return result;

        }

        public int Count()
        {
            Monitor.Enter(this);
            var result = _collection.Count;
            Monitor.Exit(this);
            return result;
        }

        public bool Contains(string key)
        {
            Monitor.Enter(this);
            var result = _collection.ContainsKey(key);
            Monitor.Exit(this);
            return result;
        }

        public object Get(string key)
        {
            Monitor.Enter(this);
            var result = _collection[key];
            result.GetCount++;
            result.LastGetDate = DateTime.UtcNow;
            Monitor.Exit(this);
            return result.Value;
        }

        public bool Remove(string key)
        {
            Monitor.Enter(this);
            var result = _collection.Remove(key);
            Monitor.Exit(this);
            return result;
        }

        public void Clear()
        {
            Monitor.Enter(this);
            _collection.Clear();
            Monitor.Exit(this);
        }

        public object? TryGet(string key)
        {
            Monitor.Enter(this);
            KbCacheItem? result = null;
            if (_collection.ContainsKey(key))
            {
                result = _collection[key];
                result.GetCount++;
                result.LastGetDate = DateTime.UtcNow;
            }
            Monitor.Exit(this);
            return result?.Value;
        }

        public bool TryGetValue(string key, out object? value)
        {
            Monitor.Enter(this);
            if (_collection.TryGetValue(key, out var result))
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
            if (_collection.ContainsKey(key))
            {
                var cacheItem = _collection[key];
                cacheItem.Value = obj;
                cacheItem.SetCount++;
                cacheItem.LastSetDate = DateTime.UtcNow;
                cacheItem.AproximateSizeInBytes = lengthOfUnserializedObject * sizeof(char);

            }
            else
            {
                _collection.Add(key, new KbCacheItem(obj, lengthOfUnserializedObject * sizeof(char)));
            }
            Monitor.Exit(this);
        }
    }
}
