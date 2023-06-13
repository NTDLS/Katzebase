using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;

namespace Katzebase.Engine.Trace
{
    internal class PerformanceTrace
    {
        internal Dictionary<string, double> Aggregations { get; private set; } = new();

        internal enum PerformanceTraceType
        {
            IndexSeek,
            IndexDistillation,
            AcquireTransaction,
            Optimization,
            Lock,
            Sampling,
            CacheRead,
            DeferredWrite,
            CacheWrite,
            IORead,
            IOWrite,
            Serialize,
            Deserialize,
            GetPBuf,
            ThreadCreation,
            ThreadQueue,
            ThreadReady,
            ThreadCompletion,
            Sorting,
            Evaluate,
            Rollback,
            Commit,
            Recording
        }

        internal TraceItem BeginTrace(PerformanceTraceType type)
        {
            return new TraceItem(this, type, $"{type}");
        }

        internal TraceItem BeginTrace(PerformanceTraceType type, string supplementalType)
        {
            return new TraceItem(this, type, $"{type}:{supplementalType}");
        }

        internal TraceItem BeginTrace<T>(PerformanceTraceType type)
        {
            return new TraceItem(this, type, $"{type}:{typeof(T).Name}");
        }

        public void Aggregate(TraceItem item)
        {
            lock (Aggregations)
            {
                if (Aggregations.ContainsKey(item.Key))
                {
                    Aggregations[item.Key] += item.Duration;
                }
                else
                {
                    Aggregations.Add(item.Key, item.Duration);
                }
            }
        }

        internal List<KbNameValue<double>> ToWaitTimes()
        {
            var result = new List<KbNameValue<double>>();

            foreach (var wt in Aggregations)
            {
                result.Add(new KbNameValue<double>(wt.Key, wt.Value));
            }

            return result;
        }

    }
}
