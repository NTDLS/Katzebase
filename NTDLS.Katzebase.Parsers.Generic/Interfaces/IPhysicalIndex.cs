namespace NTDLS.Katzebase.Parsers.Interfaces
{
    public interface IPhysicalIndex<TData> where TData : IStringable
    {
        List<IPhysicalIndexAttribute> Attributes { get; set; }
        string Name { get; set; }
        uint Partitions { get; set; }

        string GetPartitionPagesFileName(IPhysicalSchema physicalSchema, uint indexPartition);
        uint ComputePartition(TData? value);
    }
}
