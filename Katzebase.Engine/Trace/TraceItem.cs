using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Trace
{
    internal class TraceItem
    {
        public string Key { get; set; }
        public PerformanceTrace Owner { get; private set; }
        public PerformanceTraceType Type { get; private set; }
        public DateTime BeginTime { get; private set; }
        public DateTime FinishTime { get; private set; }

        /// <summary>
        /// The task duration in milliseconds.
        /// </summary>
        public double Duration { get; private set; }

        public TraceItem(PerformanceTrace owner, PerformanceTraceType type, string key)
        {
            Key = key;
            Owner = owner;
            Type = type;
            BeginTime = DateTime.UtcNow;
        }

        public void EndTrace()
        {
            FinishTime = DateTime.UtcNow;
            Duration = (FinishTime - BeginTime).TotalMilliseconds;
            Owner.Aggregate(this);
        }
        public void EndTrace(double extraTimeMilliseconds)
        {
            FinishTime = DateTime.UtcNow;
            Duration = (FinishTime - BeginTime).TotalMilliseconds + extraTimeMilliseconds;
            Owner.Aggregate(this);
        }
    }
}
