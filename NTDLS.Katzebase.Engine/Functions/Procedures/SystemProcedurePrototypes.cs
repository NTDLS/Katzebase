namespace NTDLS.Katzebase.Engine.Functions.Procedures
{
    /// <summary>
    /// Contains procedure protype definitions.
    /// </summary>
    internal class SystemProcedurePrototypes
    {
        //These are implemented in ProcedureManager.

        internal static string[] PrototypeStrings = [
                "CheckpointHealthCounters:",
                "ClearCacheAllocations:",
                "ClearHealthCounters:",
                "ReleaseCacheAllocations:",
                "ShowBlocks:Numeric/processId=null",
                "Terminate:Numeric/processId",
                "ShowVersion:Boolean/showAll=false",
                "ShowBlockTree:",
                "ShowCacheAllocations:",
                "ShowCachePages:",
                "ShowCachePartitions:",
                "ShowHealthCounters:",
                "ShowLocks:Numeric/processId=null",
                "ShowMemoryUtilization:",
                "ShowProcesses:Numeric/processId=null",
                "ShowSystemAggregateFunctions:",
                "ShowSystemProcedures:",
                "ShowSystemScalerFunctions:",
                "ShowTransactions:Numeric/processId=null",
                "ShowWaitingLocks:Numeric/processId=null",
            ];
    }
}
