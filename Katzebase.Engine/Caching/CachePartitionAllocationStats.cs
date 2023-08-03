namespace Katzebase.Engine.Caching
{
    public class CachePartitionAllocationStats
    {
        public int PartitionCount { get; set; }

        public List<CachePartitionAllocationStat> Partitions { get; private set; } = new();

        public class CachePartitionAllocationStat
        {
            public int Allocations { get; set; }
            public double SizeInKilobytes { get; set; }
        }
    }
}
