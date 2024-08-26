using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Semaphore;
using static NTDLS.Katzebase.Engine.IO.DeferredDiskIOSnapshot;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.IO
{
    internal class DeferredDiskIO
    {
        private class DeferredDiskIOObject
        {
            public string DiskPath { get; private set; }
            public object Reference { get; set; }
            public IOFormat Format { get; private set; }

            public DeferredDiskIOObject(string diskPath, object reference, IOFormat format)
            {
                DiskPath = diskPath.ToLowerInvariant();
                Reference = reference;
                Format = format;
            }
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
                    if (obj.Value.Reference != null)
                    {
                        if (obj.Value.Format == IOFormat.JSON)
                        {
                            _core.IO.PutJsonNonTrackedButCached(obj.Value.DiskPath, obj.Value.Reference);
                        }
                        else if (obj.Value.Format == IOFormat.PBuf)
                        {
                            _core.IO.PutPBufNonTrackedButCached(obj.Value.DiskPath, obj.Value.Reference);
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

        public bool GetDeferredDiskIO<T>(string key, out T? outReference)
        {
            key = key.ToLowerInvariant();

            outReference = _collection.Use(o =>
            {
                if (o.TryGetValue(key, out var deferredIO))
                {
                    return (T)deferredIO.Reference;
                }
                return default;
            });

            return outReference != null;
        }

        public void Remove(string key)
        {
            key = key.ToLowerInvariant();

            _collection.Use(o => o.Remove(key));
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
        public void PutDeferredDiskIO(string key, string diskPath, object reference, IOFormat deferredFormat)
        {
            key = key.ToLowerInvariant();

            _collection.Use(o =>
            {
                if (o.TryGetValue(key, out var value))
                {
                    value.Reference = reference;
                }
                else
                {
                    o.Add(key, new DeferredDiskIOObject(diskPath, reference, deferredFormat));
                }
            });
        }
    }
}
