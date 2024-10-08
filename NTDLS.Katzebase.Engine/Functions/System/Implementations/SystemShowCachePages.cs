﻿using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.Shared;

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
                        var values = new List<string?>
                        {
                            $"{item.Partition:n0}",
                            $"{Formatters.FileSize(item.ApproximateSizeInBytes)}",
                            $"{item.Created}",
                            $"{item.Reads:n0}",
                            $"{item.LastRead}",
                            $"{item.Writes:n0}",
                            $"{item.LastWrite}",
                            $"{page.Documents.Count:n0}",
                            $"{item.Key}"
                        };
                        result.AddRow(values);
                    }
                }
            }

            return collection;
        }
    }
}
