using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowThreadPools
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var collection = new KbQueryResultCollection();

            var result = collection.AddNew();

            result.AddField("Pool");
            result.AddField("Id");
            result.AddField("TotalProcessorTime");
            result.AddField("UserProcessorTime");
            result.AddField("PrivilegedProcessorTime");
            result.AddField("BasePriority");
            result.AddField("CurrentPriority");
            result.AddField("ThreadState");
            result.AddField("WaitReason");

            foreach (var thread in core.ThreadPool.Indexing.Threads)
            {
                var values = new List<string?>
                {
                    "Indexing",
                    $"{thread.ManagedThread.ManagedThreadId}",
                    $"{thread.NativeThread?.TotalProcessorTime.TotalMilliseconds:n0}",
                    $"{thread.NativeThread?.UserProcessorTime.TotalMilliseconds:n0}",
                    $"{thread.NativeThread?.PrivilegedProcessorTime.TotalMilliseconds:n0}",
                    $"{thread.NativeThread?.BasePriority:n0}",
                    $"{thread.NativeThread?.CurrentPriority:n0}",
                    $"{thread.NativeThread?.ThreadState}",
                    thread.NativeThread?.ThreadState == global::System.Diagnostics.ThreadState.Wait ? thread.NativeThread?.WaitReason.ToString() : string.Empty
                };

                result.AddRow(values);
            }

            foreach (var thread in core.ThreadPool.Intersection.Threads)
            {
                var values = new List<string?>
                {
                    "Intersection",
                    $"{thread.ManagedThread.ManagedThreadId}",
                    $"{thread.NativeThread?.TotalProcessorTime.TotalMilliseconds:n0}",
                    $"{thread.NativeThread?.UserProcessorTime.TotalMilliseconds:n0}",
                    $"{thread.NativeThread?.PrivilegedProcessorTime.TotalMilliseconds:n0}",
                    $"{thread.NativeThread?.BasePriority:n0}",
                    $"{thread.NativeThread?.CurrentPriority:n0}",
                    $"{thread.NativeThread?.ThreadState}",
                    thread.NativeThread?.ThreadState == global::System.Diagnostics.ThreadState.Wait ? thread.NativeThread?.WaitReason.ToString() : string.Empty
                };
                result.AddRow(values);
            }

            foreach (var thread in core.ThreadPool.Lookup.Threads)
            {
                var values = new List<string?>
                {
                    "Lookup",
                    $"{thread.ManagedThread.ManagedThreadId}",
                    $"{thread.NativeThread?.TotalProcessorTime.TotalMilliseconds:n0}",
                    $"{thread.NativeThread?.UserProcessorTime.TotalMilliseconds:n0}",
                    $"{thread.NativeThread?.PrivilegedProcessorTime.TotalMilliseconds:n0}",
                    $"{thread.NativeThread?.BasePriority:n0}",
                    $"{thread.NativeThread?.CurrentPriority:n0}",
                    $"{thread.NativeThread?.ThreadState}",
                    thread.NativeThread?.ThreadState == global::System.Diagnostics.ThreadState.Wait ? thread.NativeThread?.WaitReason.ToString() : string.Empty
                };
                result.AddRow(values);
            }

            return collection;
        }
    }
}
