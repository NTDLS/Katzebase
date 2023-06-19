using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Sessions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to sessions.
    /// </summary>
    public class SessionManager
    {
        private readonly Core core;
        internal SessionAPIHandlers APIHandlers { get; set; }
        internal SessionQueryHandlers QueryHandlers { get; set; }
        private ulong nextProcessId = 1;
        internal Dictionary<Guid, SessionState> Collection { get; set; } = new();

        public SessionManager(Core core)
        {
            this.core = core;
            APIHandlers = new SessionAPIHandlers(core);
            QueryHandlers = new SessionQueryHandlers(core);
        }

        public ulong UpsertSessionId(Guid sessionId)
        {
            lock (Collection)
            {
                if (Collection.ContainsKey(sessionId))
                {
                    return Collection[sessionId].ProcessId;
                }
                else
                {
                    ulong processId = nextProcessId++;
                    Collection.Add(sessionId, new SessionState(processId, sessionId));
                    return processId;
                }
            }
        }

        public SessionState ByProcessId(ulong sessionId)
        {
            lock (Collection)
            {
                var result = Collection.Where(o => o.Value.ProcessId == sessionId).FirstOrDefault();
                if (result.Value != null)
                {
                    return result.Value;
                }
                throw new KbSessionNotFoundException($"The session was not found: {sessionId}");
            }
        }
    }
}
