#define EnableCrudeInstrumentation

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NTDLS.Katzebase.Shared
{
    public static class CrudeInstrumentation
    {
        private static readonly InstrumentationMetrics _metrics = new();

        public class InstrumentationMetrics
        {
            public Dictionary<string, InstrumentationMetric> Collection = new();

            //QuickWatch: NTDLS.Katzebase.Shared.CrudeInstrumentation.Metrics.Ordered
            public List<KeyValuePair<string, InstrumentationMetric>> Ordered
                => Collection.OrderByDescending(static o => o.Value.Milliseconds).ToList();
        }

        public class InstrumentationMetric
        {
            public ulong Count { get; set; }
            public double Milliseconds { get; set; }
        }

        public delegate void CrudeInstrumentationProc();
        public delegate T CrudeInstrumentationProc<T>();
        public delegate T? CrudeInstrumentationNullableProc<T>();

        class MetricsTextItem
        {
            public string Milliseconds { get; set; } = string.Empty;
            public string Average { get; set; } = string.Empty;
            public string Count { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

#if !EnableCrudeInstrumentation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Witness(CrudeInstrumentationProc proc) => proc();
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Witness(CrudeInstrumentationProc proc, [CallerMemberName] string callingMethodName = "")
        {
            var sw = Stopwatch.StartNew();
            proc();
            sw.Stop();

            lock (_metrics)
            {
                if (_metrics.Collection.TryGetValue(callingMethodName, out var metrics))
                {
                    metrics.Count++;
                    metrics.Milliseconds += sw.ElapsedMilliseconds;
                }
                else
                {
                    metrics = new InstrumentationMetric()
                    {
                        Count = 1,
                        Milliseconds = sw.ElapsedMilliseconds,
                    };
                    _metrics.Collection.Add(callingMethodName, metrics);
                }
            }
        }
#endif

#if !EnableCrudeInstrumentation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Witness<T>(CrudeInstrumentationProc<T> proc) => proc();
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Witness<T>(CrudeInstrumentationProc<T> proc, [CallerMemberName] string callingMethodName = "")
        {
            var sw = Stopwatch.StartNew();
            T result = proc();
            sw.Stop();

            lock (_metrics)
            {
                if (_metrics.Collection.TryGetValue(callingMethodName, out var metrics))
                {
                    metrics.Count++;
                    metrics.Milliseconds += sw.ElapsedMilliseconds;
                }
                else
                {
                    metrics = new InstrumentationMetric()
                    {
                        Count = 1,
                        Milliseconds = sw.ElapsedMilliseconds,
                    };
                    _metrics.Collection.Add(callingMethodName, metrics);
                }
            }

            return result;
        }
#endif

#if !EnableCrudeInstrumentation
        public static T? Witness<T>(CrudeInstrumentationNullableProc<T?> proc) => proc();
#else
        public static T? Witness<T>(CrudeInstrumentationNullableProc<T?> proc, [CallerMemberName] string callingMethodName = "")
        {
            var sw = Stopwatch.StartNew();
            T? result = proc();
            sw.Stop();

            lock (_metrics)
            {
                if (_metrics.Collection.TryGetValue(callingMethodName, out var metrics))
                {
                    metrics.Count++;
                    metrics.Milliseconds += sw.ElapsedMilliseconds;
                }
                else
                {
                    metrics = new InstrumentationMetric()
                    {
                        Count = 1,
                        Milliseconds = sw.ElapsedMilliseconds,
                    };
                    _metrics.Collection.Add(callingMethodName, metrics);
                }
            }

            return result;
        }
#endif
    }
}
