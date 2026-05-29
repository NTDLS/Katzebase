using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.PersistentTypes.Atomicity;
using NTDLS.Semaphore;
using static NTDLS.Katzebase.Engine.IO.DeferredDiskIOSnapshot;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.IO
{
    internal class DeferredDiskIO
    {
        private class DeferredDiskIOObject(string diskPath, KbColumnFamilyName columnFamily, object reference, RdbKey rdbKey, IOFormat format)
        {
            public string DiskPath { get; private set; } = diskPath.ToLowerInvariant();
            public KbColumnFamilyName ColumnFamily { get; private set; } = columnFamily;
            public object Reference { get; set; } = reference;
            public RdbKey DatabaseKey { get; private set; } = rdbKey;
            public IOFormat Format { get; private set; } = format;
        }

        private EngineCore? _core;
        private readonly PessimisticCriticalResource<KbInsensitiveDictionary<DeferredDiskIOObject>> _collection = new();

        public bool ContainsKey(string key)
            => _collection.Use(o => o.ContainsKey(key));

        public void SetCore(EngineCore core)
        {
            _core = core;
        }

        public DeferredDiskIOSnapshot Snapshot()
        {
            var snapshot = new DeferredDiskIOSnapshot();

            _collection.Use(o =>
            {
                foreach (var kvp in o)
                {
                    snapshot.Collection.Add(kvp.Key, new DeferredDiskIOObjectSnapshot(kvp.Value.DiskPath, kvp.Value.Format));
                }
            });

            return snapshot;
        }

        public int Count()
            => _collection.Use(o => o.Count);

        /// <summary>
        /// Writes all deferred IOs to disk.
        /// </summary>
        public void CommitDeferredDiskIO()
        {
            _core.EnsureNotNull();

            _collection.Use(o =>
            {
                foreach (var obj in o)
                {
                    //TODO: Each deferred object is committed via a separate PutNonTrackedButCached call.
                    //If the process dies mid-loop, the commit is partial. This is where WriteBatch should be used.
                    //Accumulate all puts into a single batch per RDB path, then call db.Write(batch) once per path.

                    if (obj.Value.Reference != null)
                    {
                        var rdb = _core.IO.AcquireRdb(obj.Value.DiskPath);

                        if (obj.Value.Format == IOFormat.JSON)
                        {
                            _core.IO.PutNonTrackedButCached(rdb, obj.Value.ColumnFamily, obj.Value.DatabaseKey, obj.Value.Reference, IOFormat.JSON);
                        }
                        else if (obj.Value.Format == IOFormat.PBuf)
                        {
                            _core.IO.PutNonTrackedButCached(rdb, obj.Value.ColumnFamily, obj.Value.DatabaseKey, obj.Value.Reference, IOFormat.PBuf);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }

                o.Clear();
            });
        }

        public bool GetDeferredDiskIO<T>(CacheKey key, out T? outReference)
        {
            outReference = _collection.Use(o =>
            {
                if (o.TryGetValue(key.Canonical, out var deferredIO))
                {
                    return (T)deferredIO.Reference;
                }
                return default;
            });

            return outReference != null;
        }

        public void Remove(CacheKey key)
        {
            _collection.Use(o => o.Remove(key.Canonical));
        }

        public void RemoveItemsWithPrefix(string prefix)
        {
            prefix = prefix.ToLowerInvariant();

            if (prefix.EndsWith('\\') == false)
            {
                prefix += '\\';
            }

            _collection.Use(o =>
            {
                var keysToRemove = o.Where(o => o.Key.StartsWith(prefix)).Select(o => o.Key).ToList();

                foreach (var key in keysToRemove)
                {
                    o.Remove(key);
                }
            });
        }

        /// <summary>
        /// Keeps a reference to a file so that we can defer serializing and writing it to disk.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public void PutDeferredDiskIO(CacheKey key, string diskPath, KbColumnFamilyName columnFamily, object reference, RdbKey rdbKey, IOFormat format)
        {
            _collection.Use(o =>
            {
                if (o.TryGetValue(key.Canonical, out var value))
                {
                    value.Reference = reference;
                }
                else
                {
                    o.Add(key.Canonical, new DeferredDiskIOObject(diskPath, columnFamily, reference, rdbKey, format));
                }
            });
        }
    }
}
