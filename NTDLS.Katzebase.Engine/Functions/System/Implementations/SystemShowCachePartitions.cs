using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowCachePartitions
    {
        public static KbQueryResultCollection<TData> Execute<TData>(EngineCore<TData> core, Transaction<TData> transaction, SystemFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            var collection = new KbQueryResultCollection<TData>();
            var result = collection.AddNew();

            result.AddField("Partition");
            result.AddField("Allocations");
            result.AddField("Size");
            result.AddField("Max Size");

            var cachePartitions = core.Cache.GetPartitionAllocationStatistics();

            foreach (var partition in cachePartitions.Partitions)
            {
                var values = new List<TData> (
                new []{
                    $"{partition.Partition:n0}",
                    $"{partition.Count:n0}",
                    $"{Formatters.FileSize(partition.SizeInBytes)}",
                    $"{Formatters.FileSize(partition.Configuration.MaxMemoryBytes):n2}"
                }.Select(s=>s.CastToT<TData>(EngineCore<TData>.StrCast)));

                result.AddRow(values);
            }

            return collection;
        }
    }
}
