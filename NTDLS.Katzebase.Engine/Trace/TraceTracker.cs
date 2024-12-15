using NTDLS.Katzebase.Engine.Health;
using NTDLS.Katzebase.Engine.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Trace
{
    internal class TraceTracker : IDisposable
    {
        private TraceManager _manager;

        public TraceType TraceType { get; private set; }
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

        public TraceResult Result { get; set; }
        public string? Username { get; private set; }
        public string? ClientName { get; private set; }
        public ulong ProcessId { get; private set; }
        public Guid ConnectionId { get; private set; }

        public TraceTracker(TraceManager manager, TraceType traceType, Guid connectionId)
        {
            _manager = manager;
            TraceType = traceType;
            ConnectionId = connectionId;
        }

        public void SetSession(SessionState state)
        {
            Username = state.Username;
            ClientName = state.ClientName;
            ProcessId = state.ProcessId;
        }

        public void Dispose()
        {
            _manager.Write(this);
        }
    }
}
