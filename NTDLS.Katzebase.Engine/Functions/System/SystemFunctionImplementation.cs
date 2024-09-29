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
                "CheckpointHealthCounters()|Writes the health counters to disk. This can be useful when performance monitoring and you need to get the metrics out of the database engine and into the json file.",
                "ClearCacheAllocations()|Clears the internal memory cache collection. This does not release memory to the operating system, but leaves the memory allocated to free cache slots to be used for further cache operations. If the memory needs to be immediately freed, then use ReleaseCacheAllocations.",
                "ClearHealthCounters()|Clears the health counters that are tracked by the engine. This can be helpful when performance tuning.",
                "ReleaseCacheAllocations()|Releases unused memory from the internal memory cache back to the operating system. Call ClearCache before calling ReleaseCacheAllocations to maximize the memory that will be released.",
                "ShowAggregateFunctions()|Displays the list of built-in aggregation functions and their parameters.",
                "ShowBlocks(Numeric processId=null)|Shows blocks associated with a each process, optionally specifying a process id to filter on.",
                "ShowBlockTree()|Shows all blocking processes and their blocks as well as the resources they are waiting on.",
                "ShowCacheAllocations()|Shows the cache allocation details. This system procedure will show each file in the cache, how long its been there, how many times its been read/written and how large it is.",
                "ShowCachePages()|Much like ShowCacheAllocations, but ShowCachePages only displays information about cached database pages. Notably, this procedure also shows the count of documents in each page.",
                "ShowCachePartitions()|Shows the memory allocations by memory cache partition and the fullness of each partition.",
                "ShowHealthCounters()|Shows the health counters that are tracked by the engine.",
                "ShowLocks(Numeric processId=null)|Shows all locks, what type of lock and the object which has been locked. Optionally specifying a process id to filter by.",
                "ShowMemoryUtilization()|Shows the operating system level memory utilization used by the database engine.",
                "ShowProcesses(numeric processId=null)|Shows all active processes, their session ID, process ID and various transaction information. Optionally specifying a process id to filter on.",
                "ShowScalerFunctions()|Displays the list of built-in scaler functions and their parameters.",
                "ShowSystemFunctions()|Displays the list of built-in system functions and their parameters.",
                "ShowTransactions(Numeric processId=null)|Shows all transactions that are current active. Optionally specifying a process id.",
                "ShowVersion(Boolean showAll=false)|Shows the names and versions of all loaded assemblies.",
                "ShowWaitingLocks(Numeric processId=null)|Shows all processes that are currently waiting on a lock an provides information about those locks.",
                "Terminate(Numeric processId)|The terminate directive terminates (or kills) an existing running process. This termination will terminate any in-progress transaction, roll it back, disconnect the process and free any resources associated with it.",
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
