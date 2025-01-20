using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;
using System.Diagnostics;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowMemoryUtilization
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
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

            var process = Process.GetCurrentProcess();
            var values = new List<string?>
            {
                $"{Formatters.FileSize(process.WorkingSet64)}",
                $"{Formatters.FileSize(process.MinWorkingSet)}",
                $"{Formatters.FileSize(process.MaxWorkingSet)}",
                $"{Formatters.FileSize(process.PeakWorkingSet64)}",
                $"{Formatters.FileSize(process.PagedMemorySize64)}",
                $"{Formatters.FileSize(process.NonpagedSystemMemorySize64)}",
                $"{Formatters.FileSize(process.PeakPagedMemorySize64)}",
                $"{Formatters.FileSize(process.PeakVirtualMemorySize64)}",
                $"{Formatters.FileSize(process.VirtualMemorySize64)}",
                $"{Formatters.FileSize(process.PrivateMemorySize64)}",
            };

            result.AddRow(values);

            return collection;
        }
    }
}
