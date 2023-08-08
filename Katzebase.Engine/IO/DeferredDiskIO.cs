using Katzebase.PublicLibrary.Types;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.IO
{
    internal class DeferredDiskIO
    {
        private class DeferredDiskIOObject
        {
            public string DiskPath { get; private set; }
            public object Reference { get; set; }
            public IOFormat Format { get; private set; }
            public bool UseCompression { get; set; }

            public DeferredDiskIOObject(string diskPath, object reference, IOFormat format, bool useCompression)
            {
                DiskPath = diskPath.ToLower();
                Reference = reference;
                Format = format;
                UseCompression = useCompression;
            }
        }

        private readonly Core core;
        private readonly KbInsensitiveDictionary<DeferredDiskIOObject> Collection = new();
        public bool ContainsKey(string key) => Collection.ContainsKey(key);

        public DeferredDiskIO(Core core)
        {
            this.core = core;
        }

        public int Count()
        {
            lock (this)
            {
                return Collection.Count;
            }
        }

        /// <summary>
        /// Writes all deferred IOs to disk.
        /// </summary>
        public void CommitDeferredDiskIO()
        {
            lock (this)
            {
                foreach (var obj in Collection)
                {
                    if (obj.Value.Reference != null)
                    {
                        if (obj.Value.Format == IOFormat.JSON)
                        {
                            core.IO.PutJsonNonTracked(obj.Value.DiskPath, obj.Value.Reference, obj.Value.UseCompression);
                        }
                        else if (obj.Value.Format == IOFormat.PBuf)
                        {
                            core.IO.PutPBufNonTracked(obj.Value.DiskPath, obj.Value.Reference, obj.Value.UseCompression);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }

                Collection.Clear();
            }
        }

        public T? GetDeferredDiskIO<T>(string key)
        {
            key = key.ToLower();

            lock (this)
            {
                if (Collection.ContainsKey(key))
                {
                    return (T)Collection[key].Reference;
                }
            }
            return default;
        }

        public void Remove(string key)
        {
            key = key.ToLower();
            lock (this)
            {
                Collection.Remove(key);
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
                var keysToRemove = Collection.Where(o => o.Key.StartsWith(prefix)).Select(o => o.Key).ToList();

                foreach (var key in keysToRemove)
                {
                    Collection.Remove(key);
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
                if (Collection.ContainsKey(key))
                {
                    Collection[key].Reference = reference;
                }
                else
                {
                    Collection.Add(key, new DeferredDiskIOObject(diskPath, reference, deferredFormat, useCompression));
                }
            }
        }
    }
}