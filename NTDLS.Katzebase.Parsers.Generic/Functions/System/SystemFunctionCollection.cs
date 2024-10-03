using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Parsers.Functions.System
{
    public static class SystemFunctionCollection<TData> where TData : IStringable
    {
    	internal static string[] PrototypeStrings = {
                //Prototype Format: "functionName (parameterDataType parameterName, parameterDataType parameterName = defaultValue)"
                //Parameters that have a default specified are considered optional, they should come after non-optional parameters.
                "CheckpointHealthCounters()|'Writes the health counters to disk. This can be useful when performance monitoring and you need to get the metrics out of the database engine and into the json file.'",
                "ClearCacheAllocations()|'Clears the internal memory cache collection. This does not release memory to the operating system, but leaves the memory allocated to free cache slots to be used for further cache operations. If the memory needs to be immediately freed, then use ReleaseCacheAllocations.'",
                "ClearHealthCounters()|'Clears the health counters that are tracked by the engine. This can be helpful when performance tuning.'",
                "ReleaseCacheAllocations()|'Releases unused memory from the internal memory cache back to the operating system. Call ClearCache before calling ReleaseCacheAllocations to maximize the memory that will be released.'",
                "ShowAggregateFunctions()|'Displays the list of built-in aggregation functions and their parameters.'",
                "ShowBlocks(Numeric processId=null)|'Shows blocks associated with a each process, optionally specifying a process id to filter on.'",
                "ShowBlockTree()|'Shows all blocking processes and their blocks as well as the resources they are waiting on.'",
                "ShowCacheAllocations()|'Shows the cache allocation details. This system procedure will show each file in the cache, how long its been there, how many times its been read/written and how large it is.'",
                "ShowCachePages()|'Much like ShowCacheAllocations, but ShowCachePages only displays information about cached database pages. Notably, this procedure also shows the count of documents in each page.'",
                "ShowCachePartitions()|'Shows the memory allocations by memory cache partition and the fullness of each partition.'",
                "ShowHealthCounters()|'Shows the health counters that are tracked by the engine.'",
                "ShowLocks(Numeric processId=null)|'Shows all locks, what type of lock and the object which has been locked. Optionally specifying a process id to filter by.'",
                "ShowMemoryUtilization()|'Shows the operating system level memory utilization used by the database engine.'",
                "ShowProcesses(numeric processId=null)|'Shows all active processes, their session ID, process ID and various transaction information. Optionally specifying a process id to filter on.'",
                "ShowScalerFunctions()|'Displays the list of built-in scaler functions and their parameters.'",
                "ShowSystemFunctions()|'Displays the list of built-in system functions and their parameters.'",
                "ShowTransactions(Numeric processId=null)|'Shows all transactions that are current active. Optionally specifying a process id.'",
                "ShowVersion(Boolean showAll=false)|'Shows the names and versions of all loaded assemblies.'",
                "ShowWaitingLocks(Numeric processId=null)|'Shows all processes that are currently waiting on a lock an provides information about those locks.'",
                "Terminate(Numeric processId)|'The terminate directive terminates (or kills) an existing running process. This termination will terminate any in-progress transaction, roll it back, disconnect the process and free any resources associated with it.'",
            };
        private static List<SystemFunction<TData>>? _protypes = null;
        public static List<SystemFunction<TData>> Prototypes
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

        public static void Initialize(Func<string, TData> strParse2TData)
        {
            if (_protypes == null)
            {
                _protypes = new();

                foreach (var prototype in PrototypeStrings)
                {
                    _protypes.Add(SystemFunction<TData>.Parse(prototype, strParse2TData));
                }
            }
        }

        public static bool TryGetFunction(string name, [NotNullWhen(true)] out SystemFunction<TData>? function)
        {
            function = Prototypes.FirstOrDefault(o => o.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return function != null;
        }

        public static SystemFunctionParameterValueCollection<TData> ApplyFunctionPrototype(string functionName, List<TData?> parameters)
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
