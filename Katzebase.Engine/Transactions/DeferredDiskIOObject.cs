using System;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Transactions
{
    public class DeferredDiskIOObject
    {
        public string LowerDiskPath { get; set; }
        public string DiskPath { get; set; }
        public Object Reference { get; set; }
        public long Hits { get; set; }
        public IOFormat DeferredFormat { get; set; }
    }
}
