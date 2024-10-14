using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.System.Implementations;
using NTDLS.Katzebase.Parsers.Functions.System;

namespace NTDLS.Katzebase.Engine.Functions.System
{
    /// <summary>
    /// Contains all system function protype definitions, function implementations and expression collapse functionality.
    /// </summary>
    internal class SystemFunctionImplementation
    {
        public static KbQueryResultCollection ExecuteFunction(EngineCore core, Transaction transaction, string functionName, List<string?> parameters)
        {
            var function = SystemFunctionCollection.ApplyFunctionPrototype(functionName, parameters);

            return functionName.ToLowerInvariant() switch
            {
                "cancel" => SystemCancel.Execute(core, transaction, function),
                "checkpointhealthcounters" => SystemCheckPointHealthCounters.Execute(core, transaction, function),
                "clearcacheallocations" => SystemClearCacheAllocations.Execute(core, transaction, function),
                "clearhealthcounters" => SystemClearHealthCounters.Execute(core, transaction, function),
                "print" => SystemPrint.Execute(core, transaction, function),
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
                "showmyschemapolicy" => SystemShowMySchemaPolicy.Execute(core, transaction, function),
                "showschemapolicy" => SystemShowSchemaPolicy.Execute(core, transaction, function),
                "showprocesses" => SystemShowProcesses.Execute(core, transaction, function),
                "showscalarfunctions" => SystemShowScalarFunctions.Execute(core, transaction, function),
                "showsystemfunctions" => SystemShowSystemFunctions.Execute(core, transaction, function),
                "showthreadpools" => SystemShowThreadPools.Execute(core, transaction, function),
                "showtransactions" => SystemShowTransactions.Execute(core, transaction, function),
                "showversion" => SystemShowVersion.Execute(core, transaction, function),
                "showwaitinglocks" => SystemShowWaitingLocks.Execute(core, transaction, function),
                "refreshmyroles" => SystemRefreshMyRoles.Execute(core, transaction, function),
                "sleep" => SystemSleep.Execute(core, transaction, function),
                "terminate" => SystemTerminate.Execute(core, transaction, function),

                _ => throw new KbNotImplementedException($"The system function is not implemented: [{functionName}].")
            };
        }
    }
}
