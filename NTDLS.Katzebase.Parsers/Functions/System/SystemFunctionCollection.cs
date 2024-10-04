﻿using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Parsers.Functions.System
{
    public static class SystemFunctionCollection
    {
        internal static string[] PrototypeStrings = {
                //Prototype Format: "functionName (parameterDataType parameterName, parameterDataType parameterName = defaultValue)"
                //Parameters that have a default specified are considered optional, they should come after non-optional parameters.
                "CheckpointHealthCounters()|'Writes the health counters to disk. This can be useful when performance monitoring and you need to get the metrics out of the database engine and into the json file.'",
                "ClearCacheAllocations()|'Clears the internal memory cache collection. This does not release memory to the operating system, but leaves the memory allocated to free cache slots to be used for further cache operations. If the memory needs to be immediately freed, then use ReleaseCacheAllocations.'",
                "ClearHealthCounters()|'Clears the health counters that are tracked by the engine. This can be helpful when performance tuning.'",
                "ReleaseCacheAllocations()|'Releases unused memory from the internal memory cache back to the operating system. Call ClearCache before calling ReleaseCacheAllocations to maximize the memory that will be released.'",
                "ShowAggregateFunctions()|'Displays the list of built-in aggregation functions and their parameters.'",
                "ShowBlocks(Numeric processId=null)|'Displays blocks associated with a each process, optionally specifying a process id to filter on.'",
                "ShowBlockTree()|'Displays all blocking processes and their blocks as well as the resources they are waiting on.'",
                "ShowCacheAllocations()|'Displays the cache allocation details. This system procedure will show each file in the cache, how long its been there, how many times its been read/written and how large it is.'",
                "ShowCachePages()|'Much like ShowCacheAllocations, but ShowCachePages only displays information about cached database pages. Notably, this procedure also Displays the count of documents in each page.'",
                "ShowCachePartitions()|'Displays the memory allocations by memory cache partition and the fullness of each partition.'",
                "ShowHealthCounters()|'Displays the health counters that are tracked by the engine.'",
                "ShowLocks(Numeric processId=null)|'Displays all locks, what type of lock and the object which has been locked. Optionally specifying a process id to filter by.'",
                "ShowMemoryUtilization()|'Displays the operating system level memory utilization used by the database engine.'",
                "ShowProcesses(numeric processId=null)|'Displays all active processes, their session ID, process ID and various transaction information. Optionally specifying a process id to filter on.'",
                "ShowScalarFunctions()|'Displays the list of built-in scalar functions and their parameters.'",
                "ShowSystemFunctions()|'Displays the list of built-in system functions and their parameters.'",
                "ShowTransactions(Numeric processId=null)|'Displays all transactions that are current active. Optionally specifying a process id.'",
                "ShowVersion(Boolean showAll=false)|'Displays the names and versions of all loaded assemblies.'",
                "ShowWaitingLocks(Numeric processId=null)|'Displays all processes that are currently waiting on a lock an provides information about those locks.'",
                "Cancel(Numeric processId)|'Cancels any transaction associated with a given process, leaving the process connected.'",
                "Terminate(Numeric processId)|'Terminates an existing running process, rolls back any in-progress transaction, disconnect the process and frees any resources associated with it.'",
                "ShowThreadPools()|'Displays thread pool performance metrics.'",
            };

        private static List<SystemFunction>? _protypes = null;
        public static List<SystemFunction> Prototypes
        {
            get
            {
                if (_protypes == null)
                {
                    throw new KbFatalException("Function prototypes were not initialized.");
                }
                return _protypes;
            }
        }

        public static void Initialize()
        {
            if (_protypes == null)
            {
                _protypes = new();

                foreach (var prototype in PrototypeStrings)
                {
                    _protypes.Add(SystemFunction.Parse(prototype));
                }
            }
        }

        public static bool TryGetFunction(string name, [NotNullWhen(true)] out SystemFunction? function)
        {
            function = Prototypes.FirstOrDefault(o => o.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return function != null;
        }

        public static SystemFunctionParameterValueCollection ApplyFunctionPrototype(string functionName, List<string?> parameters)
        {
            if (_protypes == null)
            {
                throw new KbFatalException("Function prototypes were not initialized.");
            }

            var function = _protypes.FirstOrDefault(o => o.Name.Is(functionName))
                ?? throw new KbFunctionException($"Undefined system function: [{functionName}].");

            return function.ApplyParameters(parameters);
        }
    }
}
