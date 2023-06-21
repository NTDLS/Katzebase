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

        public List<SessionState> GetExpiredSessions()
        {
            lock (Collection)
            {
                var sessions = Collection.Where(o => (DateTime.UtcNow - o.Value.LastCheckinTime)
                    .TotalSeconds > core.Settings.MaxIdleConnectionSeconds).Select(o => o.Value).ToList();

                return sessions;
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
                        var session = Collection[sessionId];

                        session.LastCheckinTime = DateTime.UtcNow;

                        return session.ProcessId;
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

        /// <summary>
        /// Kills all referenced sessions and any associated transactions.
        /// </summary>
        /// <param name="processIDs"></param>
        internal void CloseByProcessIDs(List<ulong> processIDs)
        {
            try
            {
                lock (Collection)
                {
                    core.Transactions.CloseByProcessIDs(processIDs);

                    var expiredSessions = Collection.Where(o => processIDs.Contains(o.Value.ProcessId)).ToList();

                    foreach (var expiredSession in expiredSessions)
                    {
                        Collection.Remove(expiredSession.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to remove sessions by processIDs.", ex);
                throw;
            }
        }

        /// <summary>
        /// Kills a session and any associated transaction. This is how we kill a process.
        /// </summary>
        /// <param name="processId"></param>
        public void CloseByProcessId(ulong processId)
        {
            try
            {
                lock (Collection)
                {
                    core.Transactions.CloseByProcessIDs(new List<ulong> { processId });

                    var session = Collection.Where(o => o.Value.ProcessId == processId).FirstOrDefault().Value;
                    if (session != null)
                    {
                        Collection.Remove(session.SessionId);
                    }

                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to remove sessions by processIDs.", ex);
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
