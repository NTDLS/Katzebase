using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Atomicity
{
    internal class DeferredDiskIO
    {
        private Core core;
        public Dictionary<string, DeferredDiskIOObject> Collection = new Dictionary<string, DeferredDiskIOObject>();

        public DeferredDiskIO(Core core)
        {
            this.core = core;
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
                        if (deferred.Value.DeferredFormat == IOFormat.JSON)
                        {
                            core.IO.PutJsonNonTracked(deferred.Value.DiskPath, deferred.Value.Reference);
                        }
                        else if (deferred.Value.DeferredFormat == IOFormat.PBuf)
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

        /// <summary>
        /// Keeps a reference to a file so that we can defer serializing and writing it to disk.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        public bool RecordDeferredDiskIO(string key, string diskPath, object reference, IOFormat deferredFormat)
        {
            lock (this)
            {
                if (Collection.ContainsKey(key))
                {
                    var wrapper = Collection[key];
                    wrapper.Hits++;
                    wrapper.Reference = reference;
                    wrapper.DeferredFormat = deferredFormat;
                }
                else
                {
                    var wrapper = new DeferredDiskIOObject(diskPath, reference)
                    {
                        Hits = 1,
                        DeferredFormat = deferredFormat
                    };

                    Collection.Add(key, wrapper);
                }

                return true;
            }
        }
    }
}