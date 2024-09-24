using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using System.Diagnostics;
using fs;
namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowMemoryUtilization
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var cachePartitions = core.Cache.GetPartitionAllocationDetails();
            long totalCacheSize = 0;
            foreach (var partition in cachePartitions.Items)
            {
                totalCacheSize += partition.ApproximateSizeInBytes;
            }

            var collection = new KbQueryResultCollection();
            var result = collection.AddNew();
            result.AddField("Working Set");
            result.AddField("Min. Working Set");
            result.AddField("Max. WorkingSet");
            result.AddField("Peak Working Set");
            result.AddField("Paged Memory");
            result.AddField("Non-paged System Memory");
            result.AddField("Peak Paged Memory");
            result.AddField("Peak Virtual Memory");
            result.AddField("Virtual Memory");
            result.AddField("Private Memory");
            result.AddField("Cache Size");

            var process = Process.GetCurrentProcess();

            var values = new List<fstring?>
            {
                fstring.NewS($"{Formatters.FileSize(process.WorkingSet64)}"),
                fstring.NewS($"{Formatters.FileSize(process.MinWorkingSet)}"),
                fstring.NewS($"{Formatters.FileSize(process.MaxWorkingSet)}"),
                fstring.NewS($"{Formatters.FileSize(process.PeakWorkingSet64)}"),
                fstring.NewS($"{Formatters.FileSize(process.PagedMemorySize64)}"),
                fstring.NewS($"{Formatters.FileSize(process.NonpagedSystemMemorySize64)}"),
                fstring.NewS($"{Formatters.FileSize(process.PeakPagedMemorySize64)}"),
                fstring.NewS($"{Formatters.FileSize(process.PeakVirtualMemorySize64)}"),
                fstring.NewS($"{Formatters.FileSize(process.VirtualMemorySize64)}"),
                fstring.NewS($"{Formatters.FileSize(process.PrivateMemorySize64)}"),
                fstring.NewS($"{Formatters.FileSize(totalCacheSize)}"),
            };

            result.AddRow(values);

            return collection;
        }
    }
}
