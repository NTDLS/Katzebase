using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Types;
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
            public bool UseCompression { get; private set; }

            public DeferredDiskIOObject(string diskPath, object reference, IOFormat format, bool useCompression)
            {
                DiskPath = diskPath.ToLower();
                Reference = reference;
                Format = format;
                UseCompression = useCompression;
            }
        }

        private EngineCore? _core;
        private readonly KbInsensitiveDictionary<DeferredDiskIOObject> _collection = new();

        public bool ContainsKey(string key) => _collection.ContainsKey(key);

        public void SetCore(EngineCore core)
        {
            _core = core;
        }

        public DeferredDiskIOSnapshot Snapshot()
        {
            var snapshot = new DeferredDiskIOSnapshot();

            lock (this)
            {
                foreach (var kvp in _collection)
                {
                    snapshot.Collection.Add(kvp.Key, new DeferredDiskIOObjectSnapshot(kvp.Value.DiskPath, kvp.Value.Format, kvp.Value.UseCompression));
                }
            }

            return snapshot;
        }

        public int Count()
        {
            lock (this)
            {
                return _collection.Count;
            }
        }

        /// <summary>
        /// Writes all deferred IOs to disk.
        /// </summary>
        public void CommitDeferredDiskIO()
        {
            KbUtility.EnsureNotNull(_core);

            lock (this)
            {
                foreach (var obj in _collection)
                {
                    if (obj.Value.Reference != null)
                    {
                        if (obj.Value.Format == IOFormat.JSON)
                        {
                            _core.IO.PutJsonNonTracked(obj.Value.DiskPath, obj.Value.Reference, obj.Value.UseCompression);
                        }
                        else if (obj.Value.Format == IOFormat.PBuf)
                        {
                            _core.IO.PutPBufNonTracked(obj.Value.DiskPath, obj.Value.Reference, obj.Value.UseCompression);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }

                _collection.Clear();
            }
        }

        public T? GetDeferredDiskIO<T>(string key)
        {
            key = key.ToLower();

            lock (this)
            {
                if (_collection.ContainsKey(key))
                {
                    return (T)_collection[key].Reference;
                }
            }
            return default;
        }

        public void Remove(string key)
        {
            key = key.ToLower();
            lock (this)
            {
                _collection.Remove(key);
            }
        }

        public void RemoveItemsWithPrefix(string prefix)
        {
            prefix = prefix.ToLower();

            if (prefix.EndsWith("\\") == false)
            {
                prefix += "\\";
            }

            lock (this)
            {
                var keysToRemove = _collection.Where(o => o.Key.StartsWith(prefix)).Select(o => o.Key).ToList();

                foreach (var key in keysToRemove)
                {
                    _collection.Remove(key);
                }
            }
        }

        /// <summary>
        /// Keeps a reference to a file so that we can defer serializing and writing it to disk.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public void PutDeferredDiskIO(string key, string diskPath, object reference, IOFormat deferredFormat, bool useCompression)
        {
            key = key.ToLower();

            lock (this)
            {
                if (_collection.ContainsKey(key))
                {
                    _collection[key].Reference = reference;
                }
                else
                {
                    _collection.Add(key, new DeferredDiskIOObject(diskPath, reference, deferredFormat, useCompression));
                }
            }
        }
    }
}