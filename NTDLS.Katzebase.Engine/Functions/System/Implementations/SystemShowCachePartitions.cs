using fs;
using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowCachePartitions
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var collection = new KbQueryResultCollection();
            var result = collection.AddNew();

            result.AddField("Partition");
            result.AddField("Allocations");
            result.AddField("Size");
            result.AddField("Max Size");

            var cachePartitions = core.Cache.GetPartitionAllocationStatistics();

            foreach (var partition in cachePartitions.Partitions)
            {
                var values = new List<fstring?>
                {
                    fstring.NewS($"{partition.Partition:n0}"),
                    fstring.NewS($"{partition.Count:n0}"),
                    fstring.NewS($"{Formatters.FileSize(partition.SizeInBytes)}"),
                    fstring.NewS($"{Formatters.FileSize(partition.Configuration.MaxMemoryBytes):n2}")
                };

                result.AddRow(values);
            }

            return collection;
        }
    }
}
