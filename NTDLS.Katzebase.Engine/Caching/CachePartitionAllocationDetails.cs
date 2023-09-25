namespace Katzebase.Engine.Caching
{
    public class CachePartitionAllocationDetails
    {
        public int PartitionCount { get; set; }

        public List<CachePartitionAllocationDetail> Partitions { get; private set; } = new();

        public class CachePartitionAllocationDetail
        {
            public CachePartitionAllocationDetail(string key)
            {
                Key = key;
            }
            public string Key { get; set; }
            public int Partition { get; set; }
            public int AproximateSizeInBytes { get; set; }
            public ulong GetCount { get; set; } = 0;
            public ulong SetCount { get; set; } = 0;
            public DateTime? Created { get; set; }
            public DateTime? LastSetDate { get; set; }
            public DateTime? LastGetDate { get; set; }
        }
    }
}
