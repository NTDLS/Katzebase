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
                "checkpointhealthcounters" => CheckPointHealthCounters.Execute(core, transaction, function),
                "clearcacheallocations" => ClearCacheAllocations.Execute(core, transaction, function),
                "clearhealthcounters" => ClearHealthCounters.Execute(core, transaction, function),
                "releasecacheallocations" => ReleaseCacheAllocations.Execute(core, transaction, function),
                "showaggregatefunctions" => ShowAggregateFunctions.Execute(core, transaction, function),
                "showblocks" => ShowBlocks.Execute(core, transaction, function),
                "showblocktree" => ShowBlockTree.Execute(core, transaction, function),
                "showcacheallocations" => ShowCacheAllocations.Execute(core, transaction, function),
                "showcachepages" => ShowCachePages.Execute(core, transaction, function),
                "showcachepartitions" => ShowCachePartitions.Execute(core, transaction, function),
                "showhealthcounters" => ShowHealthCounters.Execute(core, transaction, function),
                "showlocks" => ShowLocks.Execute(core, transaction, function),
                "showmemoryutilization" => ShowMemoryUtilization.Execute(core, transaction, function),
                "showprocesses" => ShowProcesses.Execute(core, transaction, function),
                "showscalerfunctions" => ShowScalerFunctions.Execute(core, transaction, function),
                "showsystemfunctions" => ShowSystemFunctions.Execute(core, transaction, function),
                "showtransactions" => ShowTransactions.Execute(core, transaction, function),
                "showversion" => ShowVersion.Execute(core, transaction, function),
                "showwaitinglocks" => ShowWaitingLocks.Execute(core, transaction, function),
                "terminate" => Terminate.Execute(core, transaction, function),

                _ => throw new KbParserException($"The system function is not implemented: [{functionName}].")
            };
        }
    }
}
