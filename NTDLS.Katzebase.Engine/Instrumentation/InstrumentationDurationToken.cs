using System.Diagnostics;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;

namespace NTDLS.Katzebase.Engine.Instrumentation
{
    internal class InstrumentationDurationToken(InstrumentationTracker owner, PerformanceCounter type, string key)
    {
        public string Key { get; private set; } = key;
        public InstrumentationTracker Owner { get; private set; } = owner;
        public PerformanceCounter Type { get; private set; } = type;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public double Duration => _stopwatch.ElapsedMilliseconds;

        public void StopAndAccumulate()
        {
            _stopwatch.Stop();
            Owner.AccumulateDuration(this);
        }

        public void StopAndAccumulate(double extraTimeMilliseconds)
        {
            _stopwatch.Stop();
            Owner.AccumulateDuration(this);
        }
    }
}
