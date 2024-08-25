using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Semaphore;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Trace
{
    internal class PerformanceTrace
    {
        private readonly OptimisticCriticalResource<Dictionary<string, KbMetric>> _metrics = new();

        internal enum PerformanceTraceCumulativeMetricType
        {
            IndexSearch,
            IndexDistillation,
            DocumentPointerUnion,
            DocumentPointerIntersect,
            AcquireTransaction,
            Optimization,
            Lock,
            Sampling,
            CacheRead,
            DeferredWrite,
            DeferredRead,
            CacheWrite,
            IORead,
            IOWrite,
            Serialize,
            Deserialize,
            GetPBuf,
            ThreadCreation,
            ThreadQueue,
            ThreadDeQueue,
            ThreadReady,
            ThreadCompletion,
            Sorting,
            Evaluate,
            Rollback,
            Commit,
            Recording
        }

        internal enum PerformanceTraceDiscreteMetricType
        {
            ThreadCount,
            TransactionDuration
        }

        internal PerformanceTraceDurationTracker CreateDurationTracker(PerformanceTraceCumulativeMetricType type)
            => new(this, type, $"{type}");

        internal PerformanceTraceDurationTracker CreateDurationTracker(PerformanceTraceCumulativeMetricType type, string supplementalType)
            => new(this, type, $"{type}:{supplementalType}");

        internal PerformanceTraceDurationTracker CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType type)
            => new(this, type, $"{type}:{typeof(T).Name}");

        public void AccumulateDuration(PerformanceTraceDurationTracker item)
        {
            _metrics.Write(o =>
            {
                if (o.TryGetValue(item.Key, out var metric))
                {
                    var lookup = metric;
                    lookup.Value += item.Duration;
                    lookup.Count++;
                }
                else
                {
                    o.Add(item.Key, new KbMetric(KbMetricType.Cumulative, item.Key, item.Duration) { Count = 1 });
                }
            });
        }

        public void AddDiscreteMetric(PerformanceTraceDiscreteMetricType type, double eventValue)
        {
            _metrics.Write(o =>
            {
                var key = $"{type}";

                if (o.TryGetValue(key, out var metric))
                {
                    var lookup = metric;
                    lookup.Value = eventValue;
                    lookup.Count++;
                }
                else
                {
                    o.Add(key, new KbMetric(KbMetricType.Discrete, key, eventValue) { Count = 1 });
                }
            });
        }

        internal KbMetricCollection ToCollection()
        {
            return _metrics.Read(o =>
            {
                var result = new KbMetricCollection();
                result.AddRange(o.Select(o => o.Value));
                return result;
            });
        }
    }
}
