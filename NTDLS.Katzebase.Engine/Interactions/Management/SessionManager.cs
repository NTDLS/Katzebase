using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Engine.Sessions;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to sessions.
    /// </summary>
    public class SessionManager
    {
        private readonly Core _core;
        private ulong _nextProcessId = 1;

        internal SessionAPIHandlers APIHandlers { get; private set; }
        internal SessionQueryHandlers QueryHandlers { get; private set; }
        internal Dictionary<Guid, SessionState> Collection { get; private set; } = new();

        public SessionManager(Core core)
        {
            _core = core;
            try
            {
                APIHandlers = new SessionAPIHandlers(core);
                QueryHandlers = new SessionQueryHandlers(core);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instantiate session manager.", ex);
                throw;
            }
        }

        public Dictionary<Guid, SessionState> CloneSessions()
        {
            Monitor.Enter(Collection);
            try
            {
                return Collection.ToDictionary(o => o.Key, o => o.Value);
            }
            finally
            {
                Monitor.Exit(Collection);
            }
        }

        public List<SessionState> GetExpiredSessions()
        {
            Monitor.Enter(Collection);
            try
            {
                return Collection.Where(o => (DateTime.UtcNow - o.Value.LastCheckinTime)
                    .TotalSeconds > _core.Settings.MaxIdleConnectionSeconds).Select(o => o.Value).ToList();
            }
            finally
            {
                Monitor.Exit(Collection);
            }
        }

        public ulong UpsertSessionId(Guid sessionId, string clientName = "")
        {
            Monitor.Enter(Collection);

            try
            {
                if (Collection.ContainsKey(sessionId))
                {
                    var session = Collection[sessionId];
                    session.LastCheckinTime = DateTime.UtcNow;
                    return session.ProcessId;
                }
                else
                {
                    ulong processId = _nextProcessId++;

                    var session = new SessionState(processId, sessionId)
                    {
                        ClientName = clientName
                    }
                    ;
                    Collection.Add(sessionId, session);
                    return processId;
                }

            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to upsert session for session {sessionId}.", ex);
                throw;
            }
            finally
            {
                Monitor.Exit(Collection);
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
                _core.Transactions.CloseByProcessID(processId);

                //Once the transaction for the process has been closed, removing the process is a non-critical task.
                // For this reason, we will "try lock" with a timeout, if we fail to remove the session now - it will be
                // automatically retried by the HeartbeatManager.
                bool lockAcquired = Monitor.TryEnter(Collection, TimeSpan.FromSeconds(1));

                try
                {
                    if (lockAcquired)
                    {
                        var session = Collection.Where(o => o.Value.ProcessId == processId).FirstOrDefault().Value;
                        if (session != null)
                        {
                            Collection.Remove(session.SessionId);
                        }
                    }
                    else
                    {
                        _core.Log.Write($"Lock timeout expired while removing session. The task will be deferred to the heartbeat manager.", Client.KbConstants.KbLogSeverity.Warning);
                    }
                }
                finally
                {
                    if (lockAcquired)
                    {
                        Monitor.Exit(Collection);
                    }
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to remove sessions by processIDs.", ex);
                throw;
            }
        }

        public SessionState ByProcessId(ulong processId)
        {
            Monitor.Enter(Collection);
            try
            {
                var result = Collection.Where(o => o.Value.ProcessId == processId).FirstOrDefault();
                if (result.Value != null)
                {
                    return result.Value;
                }
                throw new KbSessionNotFoundException($"The session was not found: {processId}");
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to get session state by process id for process id {processId}.", ex);
                throw;
            }
            finally
            {
                Monitor.Exit(Collection);
            }
        }
    }
}
