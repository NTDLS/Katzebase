using static NTDLS.Katzebase.Engine.Trace.PerformanceTrace;

namespace NTDLS.Katzebase.Engine.Trace
{
    internal class PerformanceTraceDurationTracker
    {
        public string Key { get; private set; }
        public PerformanceTrace Owner { get; private set; }
        public PerformanceCounter Type { get; private set; }
        public DateTime BeginTime { get; private set; }
        public DateTime FinishTime { get; private set; }

        /// <summary>
        /// The task duration in milliseconds.
        /// </summary>
        public double Duration { get; private set; }

        public PerformanceTraceDurationTracker(PerformanceTrace owner, PerformanceCounter type, string key)
        {
            Key = key;
            Owner = owner;
            Type = type;
            BeginTime = DateTime.UtcNow;
        }

        public void StopAndAccumulate()
        {
            FinishTime = DateTime.UtcNow;
            Duration = (FinishTime - BeginTime).TotalMilliseconds;
            Owner.AccumulateDuration(this);
        }

        public void StopAndAccumulate(double extraTimeMilliseconds)
        {
            FinishTime = DateTime.UtcNow;
            Duration = (FinishTime - BeginTime).TotalMilliseconds + extraTimeMilliseconds;
            Owner.AccumulateDuration(this);
        }
    }
}
