using Katzebase.PublicLibrary.Exceptions;
using System.Diagnostics;

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
            try
            {
                APIHandlers = new SessionAPIHandlers(core);
                QueryHandlers = new SessionQueryHandlers(core);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate session manager.", ex);
                throw;
            }
        }

        public ulong UpsertSessionId(Guid sessionId)
        {
            try
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
            catch (Exception ex)
            {
                core.Log.Write($"Failed to upsert session for session {sessionId}.", ex);
                throw;
            }
        }

        public SessionState ByProcessId(ulong processId)
        {
            try
            {
                lock (Collection)
                {
                    var result = Collection.Where(o => o.Value.ProcessId == processId).FirstOrDefault();
                    if (result.Value != null)
                    {
                        return result.Value;
                    }
                    throw new KbSessionNotFoundException($"The session was not found: {processId}");
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to get session state by process id for process id {processId}.", ex);
                throw;
            }
        }
    }
}
