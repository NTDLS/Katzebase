namespace Katzebase.Engine.Functions.Procedures
{
    /// <summary>
    /// Contains procedure protype defintions.
    /// </summary>
    internal class QueryProcedureImplementation
    {
        internal static string[] SystemProcedurePrototypes = {
                "ClearHealthCounters:",
                "CheckpointHealthCounters:",
                "ClearCache:",
                "ReleaseCacheAllocations:",
                "ShowcachePartitions:",
                "ShowGealthCounters:",
                "ShowWaitingLocks:Integer/processId=null",
                "ShowBlocks:Integer/processId=null",
                "ShowTransactions:Integer/processId=null",
                "ShowProcesses:Integer/processId=null",
            };
    }
}
