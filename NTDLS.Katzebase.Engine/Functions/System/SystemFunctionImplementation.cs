using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.System.Implementations;

namespace NTDLS.Katzebase.Engine.Functions.System
{
    /// <summary>
    /// Contains all system function protype definitions, function implementations and expression collapse functionality.
    /// </summary>
    internal class SystemFunctionImplementation
    {
        internal static string[] PrototypeStrings = {
                //Prototype Format: "functionName (parameterDataType parameterName, parameterDataType parameterName = defaultValue)"
                //Parameters that have a default specified are considered optional, they should come after non-optional parameters.
                "CheckpointHealthCounters()",
                "ClearCacheAllocations()",
                "ClearHealthCounters()",
                "ReleaseCacheAllocations()",
                "ShowAggregateFunctions()",
                "ShowBlocks(Numeric processId=null)",
                "ShowBlockTree()",
                "ShowCacheAllocations()",
                "ShowCachePages()",
                "ShowCachePartitions()",
                "ShowHealthCounters()",
                "ShowLocks(Numeric processId=null)",
                "ShowMemoryUtilization()",
                "ShowProcesses(numeric processId=null)",
                "ShowScalerFunctions()",
                "ShowSystemAggregateFunctions()",
                "ShowSystemFunctions()",
                "ShowTransactions(Numeric processId=null)",
                "ShowVersion(Boolean showAll=false)",
                "ShowWaitingLocks(Numeric processId=null)",
                "Terminate(Numeric processId)",
            };
        /*
         * */
        public static KbQueryResultCollection ExecuteFunction<TData>(EngineCore<TData> core, Transaction<TData> transaction, string functionName, List<TData?> parameters) where TData : IStringable
        {
            var function = SystemFunctionCollection<TData>.ApplyFunctionPrototype(functionName, parameters);

            return functionName.ToLowerInvariant() switch
            {
                "checkpointhealthcounters"  => SystemCheckPointHealthCounters.Execute<TData>(core, transaction, function),
                "clearcacheallocations"     => SystemClearCacheAllocations.Execute<TData>(core, transaction, function),
                "clearhealthcounters"       => SystemClearHealthCounters.Execute<TData>(core, transaction, function),
                "releasecacheallocations"   => SystemReleaseCacheAllocations.Execute<TData>(core, transaction, function),
                "showaggregatefunctions"    => SystemShowAggregateFunctions.Execute<TData>(core, transaction, function),
                "showblocks"                => SystemShowBlocks.Execute<TData>(core, transaction, function),
                "showblocktree"             => SystemShowBlockTree.Execute<TData>(core, transaction, function),
                "showcacheallocations"      => SystemShowCacheAllocations.Execute<TData>(core, transaction, function),
                "showcachepages"            => SystemShowCachePages.Execute<TData>(core, transaction, function),
                "showcachepartitions"       => SystemShowCachePartitions.Execute<TData>(core, transaction, function),
                "showhealthcounters"        => SystemShowHealthCounters.Execute<TData>(core, transaction, function),

                "showlocks"                 => SystemShowLocks<TData>.Execute(core, transaction, function),
                "showmemoryutilization"     => SystemShowMemoryUtilization<TData>.Execute(core, transaction, function),
                "showprocesses"             => SystemShowProcesses<TData>.Execute(core, transaction, function),
                "showscalerfunctions"       => SystemShowScalerFunctions<TData>.Execute(core, transaction, function),
                "showsystemfunctions"       => SystemShowSystemFunctions<TData>.Execute(core, transaction, function),
                "showtransactions"          => SystemShowTransactions<TData>.Execute(core, transaction, function),
                "showversion"               => SystemShowVersion<TData>.Execute(core, transaction, function),

                "showwaitinglocks"          => SystemShowWaitingLocks.Execute<TData>(core, transaction, function),
                "terminate"                 => SystemTerminate.Execute<TData>(core, transaction, function),

                _ => throw new KbParserException($"The system function is not implemented: [{functionName}].")
            };
        }
    }
}
