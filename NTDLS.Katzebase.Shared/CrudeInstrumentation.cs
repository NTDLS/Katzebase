using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NTDLS.Katzebase.Shared
{
    public static class CrudeInstrumentation
    {
        public readonly static Dictionary<string, InstrumentationMetrics> Metrics = new();

        public class InstrumentationMetrics
        {
            public ulong Count { get; set; }
            public double Milliseconds { get; set; }
        }

        public delegate T CrudeInstrumentationProc<T>();
        public static T Witness<T>(CrudeInstrumentationProc<T> proc, [CallerMemberName] string callingMethodName = "")
        {
            var sw = Stopwatch.StartNew();

            T result = proc();

            lock (Metrics)
            {
                if (Metrics.TryGetValue(callingMethodName, out var metrics))
                {
                    metrics.Count++;
                    metrics.Milliseconds += sw.ElapsedMilliseconds;
                }
                else
                {
                    metrics = new InstrumentationMetrics()
                    {
                        Count = 1,
                        Milliseconds = sw.ElapsedMilliseconds,
                    };
                    Metrics.Add(callingMethodName, metrics);
                }
            }

            return result;
        }
    }
}
