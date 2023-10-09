using NTDLS.Semaphore;

namespace NTDLS.Katzebase.Engine.Caching
{
    internal class KbMemoryCache : IDisposable
    {
        private readonly CriticalResource<Dictionary<string, KbCacheItem>> _collection = new();
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
                    _collection.Use((obj) => obj.Clear());
                    _timer.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion

        public List<string> CloneKeys() => _collection.Use((obj) => obj.Select(o => o.Key).ToList());

        public Dictionary<string, KbCacheItem> CloneCollection() =>
            _collection.Use((obj) => obj.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Clone()
            ));

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
                _collection.TryUse(50, (obj) =>
                {
                    //When we reach our set memory pressure, we will remove the least recently hit items from cache.
                    //TODO: since we have the hit count, update count, etc. maybe we can make this more intelligent?

                    var oldestGottenItems = obj.OrderBy(o => o.Value.LastGetDate)
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
                });
            }
        }

        public double SizeInMegabytes() => _collection.Use((obj) => obj.Sum(o => o.Value.AproximateSizeInBytes / 1024.0 / 1024.0));
        public double MaxSizeInMegabytes() => (_core.Settings.CacheMaxMemory / _cachePartitions);
        public double MaxSizeInKilobytes() => (_core.Settings.CacheMaxMemory / _cachePartitions) * 1024.0;
        public double SizeInKilobytes() => _collection.Use((obj) => obj.Sum(o => o.Value.AproximateSizeInBytes / 1024.0));
        public int Count() => _collection.Use((obj) => obj.Count);
        public bool Contains(string key) => _collection.Use((obj) => obj.ContainsKey(key));
        public bool Remove(string key) => _collection.Use((obj) => obj.Remove(key));
        public void Clear() => _collection.Use((obj) => obj.Clear());

        public object Get(string key)
        {
            return _collection.Use((obj) =>
            {
                var result = obj[key];
                result.GetCount++;
                result.LastGetDate = DateTime.UtcNow;
                return result.Value;
            });
        }

        public object? TryGet(string key)
        {
            return _collection.Use((obj) =>
            {
                if (obj.ContainsKey(key))
                {
                    var result = obj[key];
                    result.GetCount++;
                    result.LastGetDate = DateTime.UtcNow;
                    return result?.Value;
                }
                return null;
            });
        }

        public void Upsert(string key, object value, int lengthOfUnserializedObject = 0)
        {
            _collection.Use(obj =>
            {
                if (obj.ContainsKey(key))
                {
                    var cacheItem = obj[key];
                    cacheItem.Value = obj;
                    cacheItem.SetCount++;
                    cacheItem.LastSetDate = DateTime.UtcNow;
                    cacheItem.AproximateSizeInBytes = lengthOfUnserializedObject * sizeof(char);
                }
                else
                {
                    obj.Add(key, new KbCacheItem(value, lengthOfUnserializedObject * sizeof(char)));
                }
            });
        }
    }
}
