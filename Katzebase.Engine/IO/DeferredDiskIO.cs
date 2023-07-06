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

            public DeferredDiskIOObject(string diskPath, object reference, IOFormat format)
            {
                DiskPath = diskPath.ToLower();
                Reference = reference;
                Format = format;
            }
        }

        private readonly Core core;
        private readonly Dictionary<string, DeferredDiskIOObject> Collection = new();
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
                foreach (var deferred in Collection)
                {
                    if (deferred.Value.Reference != null)
                    {
                        if (deferred.Value.Format == IOFormat.JSON)
                        {
                            core.IO.PutJsonNonTracked(deferred.Value.DiskPath, deferred.Value.Reference);
                        }
                        else if (deferred.Value.Format == IOFormat.PBuf)
                        {
                            core.IO.PutPBufNonTracked(deferred.Value.DiskPath, deferred.Value.Reference);
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

        /// <summary>
        /// Keeps a reference to a file so that we can defer serializing and writing it to disk.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public void PutDeferredDiskIO(string key, string diskPath, object reference, IOFormat deferredFormat)
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
                    Collection.Add(key, new DeferredDiskIOObject(diskPath, reference, deferredFormat));
                }
            }
        }
    }
}