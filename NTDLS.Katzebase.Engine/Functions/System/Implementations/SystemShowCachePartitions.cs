﻿using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;

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
                var values = new List<string?>
                {
                    $"{partition.Partition:n0}",
                    $"{partition.Count:n0}",
                    $"{Formatters.FileSize(partition.SizeInBytes)}",
                    $"{Formatters.FileSize(partition.Configuration.MaxMemoryBytes):n2}"
                };

                result.AddRow(values);
            }

            return collection;
        }
    }
}
