namespace NTDLS.Katzebase.Engine.Functions.Procedures
{
    /// <summary>
    /// Contains procedure protype defintions.
    /// </summary>
    internal class SystemProcedureImplementation
    {
        internal static string[] SystemProcedurePrototypes = {
                "SystemScalerFunctions:",
                "CheckpointHealthCounters:",
                "ClearCacheAllocations:",
                "ClearHealthCounters:",
                "ReleaseCacheAllocations:",
                "ShowBlocks:Numeric/processId=null",
                "ShowCacheAllocations:",
                "ShowCachePartitions:",
                "ShowHealthCounters:",
                "ShowMemoryUtilization:",
                "ShowProcesses:Numeric/processId=null",
                "ShowTransactions:Numeric/processId=null",
                "ShowWaitingLocks:Numeric/processId=null",
            };
    }
}
