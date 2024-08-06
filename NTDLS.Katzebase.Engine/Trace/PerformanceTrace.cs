using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Semaphore;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Trace
{
    internal class PerformanceTrace
    {
        private readonly OptimisticCriticalResource<KbInsensitiveDictionary<KbMetric>> _metrics = new();

        internal enum PerformanceTraceCumulativeMetricType
        {
            IndexSearch,
            IndexDistillation,
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
                if (o.ContainsKey(item.Key))
                {
                    var lookup = o[item.Key];
                    lookup.Value += item.Duration;
                    lookup.Count++;
                }
                else
                {
                    var lookup = new KbMetric(KbMetricType.Cumulative, item.Key, item.Duration);
                    lookup.Count++;
                    o.Add(item.Key, lookup);
                }
            });
        }

        public void AddDiscreteMetric(PerformanceTraceDiscreteMetricType type, double value)
        {
            _metrics.Write(o =>
            {
                var key = $"{type}";

                if (o.ContainsKey(key))
                {
                    var lookup = o[key];
                    lookup.Value = value;
                    lookup.Count++;
                }
                else
                {
                    var lookup = new KbMetric(KbMetricType.Discrete, key, value);
                    lookup.Count++;
                    o.Add(key, lookup);
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
