using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json.Linq;
using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.Engine.Trace
{
    internal class PerformanceTrace
    {
        internal Dictionary<string, KbMetric> Metrics { get; private set; } = new();

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

        internal enum PerformanceTraceDescreteMetricType
        {
            ThreadCount,
            TransactionDuration
        }

        internal PerformanceTraceDurationTracker CreateDurationTracker(PerformanceTraceCumulativeMetricType type)
        {
            return new PerformanceTraceDurationTracker(this, type, $"{type}");
        }

        internal PerformanceTraceDurationTracker CreateDurationTracker(PerformanceTraceCumulativeMetricType type, string supplementalType)
        {
            return new PerformanceTraceDurationTracker(this, type, $"{type}:{supplementalType}");
        }

        internal PerformanceTraceDurationTracker CreateDurationTracker<T>(PerformanceTraceCumulativeMetricType type)
        {
            return new PerformanceTraceDurationTracker(this, type, $"{type}:{typeof(T).Name}");
        }

        public void AccumulateDuration(PerformanceTraceDurationTracker item)
        {

            lock (Metrics)
            {
                if (Metrics.ContainsKey(item.Key))
                {
                    var lookup = Metrics[item.Key];
                    lookup.Value += item.Duration;
                    lookup.Count++;
                }
                else
                {
                    var lookup = new KbMetric(KbMetricType.Cumulative, item.Key, item.Duration);
                    lookup.Count++;
                    Metrics.Add(item.Key, lookup);
                }
            }
        }

        public void AddDescreteMetric(PerformanceTraceDescreteMetricType type, double value)
        {
            lock (Metrics)
            {
                var key = $"{type}";

                if (Metrics.ContainsKey(key))
                {
                    var lookup = Metrics[key];
                    lookup.Value = value;
                    lookup.Count++;
                }
                else
                {
                    var lookup = new KbMetric(KbMetricType.Descrete, key, value);
                    lookup.Count++;
                    Metrics.Add(key, lookup);
                }
            }
        }

        internal KbMetricCollection ToCollection()
        {
            var result = new KbMetricCollection();

            result.AddRange(Metrics.Select(o => o.Value));

            return result;
        }
    }
}
