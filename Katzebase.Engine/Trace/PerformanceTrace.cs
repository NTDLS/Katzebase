using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Trace
{
    public class PerformanceTrace
    {
        public Dictionary<string, double> Aggregations { get; private set; } = new();

        public enum PerformanceTraceType
        {
            IndexSeek,
            IndexDistillation,
            AcquireTransaction,
            Optimization,
            Lock,
            Sampling,
            CacheRead,
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
            Evaluate
        }

        public TraceItem BeginTrace(PerformanceTraceType type)
        {
            if (type == PerformanceTraceType.Lock)
            {
                throw new KbFatalException("Lock trace requires sub type. Use BeginTrace<T>().");
            }

            return new TraceItem(this, type, $"{type}");
        }

        public TraceItem BeginTrace<T>(PerformanceTraceType type)
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
    }
}
