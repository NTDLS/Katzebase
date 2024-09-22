using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowCacheAllocations
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var collection = new KbQueryResultCollection();
            var result = collection.AddNew();
            result.AddField("Partition");
            result.AddField("Approximate Size");
            result.AddField("Created");
            result.AddField("Reads");
            result.AddField("Last Read");
            result.AddField("Writes");
            result.AddField("Last Write");
            result.AddField("Key");

            var cachePartitions = core.Cache.GetPartitionAllocationDetails();

            foreach (var item in cachePartitions.Items)
            {
                var values = new List<string?>
                {
                    $"{item.Partition:n0}",
                    $"{Formatters.FileSize(item.ApproximateSizeInBytes)}",
                    $"{item.Created}",
                    $"{item.Reads:n0}",
                    $"{item.LastRead}",
                    $"{item.Writes:n0}",
                    $"{item.LastWrite}",
                    $"{item.Key}",
                };

                result.AddRow(values);
            }

            return collection;
        }
    }
}
