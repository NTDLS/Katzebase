using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Parsers.Interfaces;
using NTDLS.Katzebase.Parsers.Functions.System;
namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    public static class SystemShowCachePages
    {
        public static KbQueryResultCollection<TData> Execute<TData>(EngineCore<TData> core, Transaction<TData> transaction, SystemFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            var collection = new KbQueryResultCollection<TData>();
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
                    if (pageObject is PhysicalDocumentPage<TData> page)
                    {
                        var values = new List<TData?>(
                        new[]{
                            $"{item.Partition:n0}",
                            $"{Formatters.FileSize(item.ApproximateSizeInBytes)}",
                            $"{item.Created}",
                            $"{item.Reads:n0}",
                            $"{item.LastRead}",
                            $"{item.Writes:n0}",
                            $"{item.LastWrite}",
                            $"{page.Documents.Count:n0}",
                            $"{item.Key}"
                        }.Select(s => s.CastToT<TData>(EngineCore<TData>.StrCast)));
                        result.AddRow(values);
                    }
                }
            }

            return collection;
        }
    }
}
