using NTDLS.Katzebase.Engine.Trace;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Trace
{
    internal class TraceManager
    {
        private readonly EngineCore _core;

        public TraceManager(EngineCore core)
        {
            _core = core;
        }

        public TraceTracker CreateTracker(TraceType traceType, Guid connectionId)
        {
            return new TraceTracker(this, traceType, connectionId);
        }

        public void Write(TraceTracker tracker)
        {
            Console.WriteLine($"Trace: {tracker.TraceType} {tracker.ProcessId} {tracker.Timestamp}");
        }
    }
}
