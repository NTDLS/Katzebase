using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Atomicity
{
    public class DeferredDiskIOObject
    {
        public string DiskPath { get; set; }
        public object Reference { get; set; }
        public long Hits { get; set; } = 0;
        public IOFormat DeferredFormat { get; set; }

        public DeferredDiskIOObject(string diskPath, object reference)
        {
            DiskPath = diskPath.ToLower();
            Reference = reference;
        }
    }
}
