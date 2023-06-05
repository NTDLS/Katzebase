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
            Evaluate
        }

        public TraceItem BeginTrace(PerformanceTraceType type)
        {
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
