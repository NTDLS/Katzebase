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
        public static KbQueryResultCollection ExecuteFunction(EngineCore core, Transaction transaction, string functionName, List<string?> parameters)
        {
            var function = SystemFunctionCollection.ApplyFunctionPrototype(functionName, parameters);

            return functionName.ToLowerInvariant() switch
            {
                "checkpointhealthcounters" => SystemCheckPointHealthCounters.Execute(core, transaction, function),
                "clearcacheallocations" => SystemClearCacheAllocations.Execute(core, transaction, function),
                "clearhealthcounters" => SystemClearHealthCounters.Execute(core, transaction, function),
                "releasecacheallocations" => SystemReleaseCacheAllocations.Execute(core, transaction, function),
                "showaggregatefunctions" => SystemShowAggregateFunctions.Execute(core, transaction, function),
                "showblocks" => SystemShowBlocks.Execute(core, transaction, function),
                "showblocktree" => SystemShowBlockTree.Execute(core, transaction, function),
                "showcacheallocations" => SystemShowCacheAllocations.Execute(core, transaction, function),
                "showcachepages" => SystemShowCachePages.Execute(core, transaction, function),
                "showcachepartitions" => SystemShowCachePartitions.Execute(core, transaction, function),
                "showhealthcounters" => SystemShowHealthCounters.Execute(core, transaction, function),
                "showlocks" => SystemShowLocks.Execute(core, transaction, function),
                "showmemoryutilization" => SystemShowMemoryUtilization.Execute(core, transaction, function),
                "showprocesses" => SystemShowProcesses.Execute(core, transaction, function),
                "showscalerfunctions" => SystemShowScalerFunctions.Execute(core, transaction, function),
                "showsystemfunctions" => SystemShowSystemFunctions.Execute(core, transaction, function),
                "showtransactions" => SystemShowTransactions.Execute(core, transaction, function),
                "showversion" => SystemShowVersion.Execute(core, transaction, function),
                "showwaitinglocks" => SystemShowWaitingLocks.Execute(core, transaction, function),
                "terminate" => SystemTerminate.Execute(core, transaction, function),

                _ => throw new KbParserException($"The system function is not implemented: [{functionName}].")
            };
        }
    }
}
