namespace NTDLS.Katzebase.Engine.Functions.Procedures
{
    /// <summary>
    /// Contains procedure protype definitions.
    /// </summary>
    internal class SystemProcedurePrototypes
    {
        //These are implemented in ProcedureManager.

        internal static string[] PrototypeStrings = {
                "SystemAggregateFunctions:",
                "SystemProcedures:",
                "SystemScalerFunctions:",
                "CheckpointHealthCounters:",
                "ClearCacheAllocations:",
                "ClearHealthCounters:",
                "ReleaseCacheAllocations:",
                "ShowBlocks:Numeric/processId=null",
                "ShowLocks:Numeric/processId=null",
                "ShowCacheAllocations:",
                "ShowCachePartitions:",
                "ShowCachePages:",
                "ShowHealthCounters:",
                "ShowMemoryUtilization:",
                "ShowBlockTree:",
                "ShowProcesses:Numeric/processId=null",
                "ShowTransactions:Numeric/processId=null",
                "ShowWaitingLocks:Numeric/processId=null",
            };
    }
}
