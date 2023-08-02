namespace Katzebase.Engine.Caching
{
    public class PartitionAllocationsDetails
    {
        public int PartitionCount { get; set; }

        public List<PartitionAllocationDetails> Partitions { get; private set; } = new();

        public class PartitionAllocationDetails
        {
            public int Allocations { get; set; }
            public double SizeInKilobytes { get; set; }
        }
    }
}
