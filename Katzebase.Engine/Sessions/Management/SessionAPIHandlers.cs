using Katzebase.Engine.Query;
using Katzebase.Engine.Trace;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.Sessions.SessionState;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Sessions.Management
{
    internal class SessionAPIHandlers
    {
        private readonly Core core;

        public SessionAPIHandlers(Core core)
        {
            this.core = core;
        }
    }
}
