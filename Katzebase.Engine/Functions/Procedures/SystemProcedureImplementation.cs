namespace Katzebase.Engine.Functions.Procedures
{
    /// <summary>
    /// Contains procedure protype defintions.
    /// </summary>
    internal class SystemProcedureImplementation
    {
        internal static string[] SystemProcedurePrototypes = {
                "ClearHealthCounters:",
                "CheckpointHealthCounters:",
                "ClearCache:",
                "ReleaseAllocations:",
                "ShowMemoryUtilization:",
                "ShowCacheAllocations:",
                "ShowCachePartitions:",
                "ShowhealthCounters:",
                "ShowWaitingLocks:Numeric/processId=null",
                "ShowBlocks:Numeric/processId=null",
                "ShowTransactions:Numeric/processId=null",
                "ShowProcesses:Numeric/processId=null",
            };
    }
}
