using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Library;
using fs;
namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowCachePages
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
            result.AddField("Documents");
            result.AddField("Key");

            var cachePartitions = core.Cache.GetPartitionAllocationDetails();

            foreach (var item in cachePartitions.Items.Where(o => o.Key.EndsWith(EngineConstants.DocumentPageExtension)))
            {
                if (core.Cache.TryGet(item.Key, out var pageObject))
                {
                    if (pageObject is PhysicalDocumentPage page)
                    {
                        var values = new List<fstring?>
                        {
                            fstring.NewS($"{item.Partition:n0}"),
                            fstring.NewS($"{Formatters.FileSize(item.ApproximateSizeInBytes)}"),
                            fstring.NewS($"{item.Created}"),
                            fstring.NewS($"{item.Reads:n0}"),
                            fstring.NewS($"{item.LastRead}"),
                            fstring.NewS($"{item.Writes:n0}"),
                            fstring.NewS($"{item.LastWrite}"),
                            fstring.NewS($"{page.Documents.Count:n0}"),
                            fstring.NewS($"{item.Key}")
                        };
                        result.AddRow(values);
                    }
                }
            }

            return collection;
        }
    }
}
