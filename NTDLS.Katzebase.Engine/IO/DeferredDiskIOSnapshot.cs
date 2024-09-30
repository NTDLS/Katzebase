using NTDLS.Katzebase.Client.Types;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.IO
{
    /// <summary>
    /// Snapshot class for DeferredDiskIO, used to snapshot the state of the associated class.
    /// </summary>
    internal class DeferredDiskIOSnapshot
    {
        /// <summary>
        /// Snapshot class for DeferredDiskIOObject, used to snapshot the state of the associated class.
        /// </summary>
        public class DeferredDiskIOObjectSnapshot
        {
            public string DiskPath { get; private set; }
            public IOFormat Format { get; private set; }

            public DeferredDiskIOObjectSnapshot(string diskPath, IOFormat format)
            {
                DiskPath = diskPath.ToLowerInvariant();
                Format = format;
            }
        }

        public KbInsensitiveDictionary<DeferredDiskIOObjectSnapshot> Collection { get; } = new();

        public bool ContainsKey(string key) => Collection.ContainsKey(key);

        public DeferredDiskIOSnapshot()
        {
        }

        public int Count()
        {
            lock (this)
            {
                return Collection.Count;
            }
        }
    }
}
