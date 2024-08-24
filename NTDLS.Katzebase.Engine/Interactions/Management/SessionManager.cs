using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Semaphore;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to sessions.
    /// </summary>
    public class SessionManager
    {
        private readonly EngineCore _core;
        private ulong _nextProcessId = 1;
        private readonly OptimisticCriticalResource<Dictionary<Guid, SessionState>> _collection = new();

        public SessionAPIHandlers APIHandlers { get; private set; }
        internal SessionQueryHandlers QueryHandlers { get; private set; }

        internal SessionManager(EngineCore core)
        {
            _core = core;
            try
            {
                APIHandlers = new SessionAPIHandlers(core);
                QueryHandlers = new SessionQueryHandlers(core);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate session manager.", ex);
                throw;
            }
        }

        internal Dictionary<Guid, SessionState> CloneSessions()
        {
            return _collection.Read((obj) => obj.ToDictionary(o => o.Key, o => o.Value));
        }

        internal SessionState UpsertConnectionId(Guid connectionId, string clientName = "")
        {
            return _collection.Write((obj) =>
            {
                try
                {
                    if (obj.TryGetValue(connectionId, out SessionState? value))
                    {
                        var session = value;
                        session.LastCheckInTime = DateTime.UtcNow;
                        return session;
                    }
                    else
                    {
                        ulong processId = _nextProcessId++;

                        var session = new SessionState(processId, connectionId)
                        {
                            ClientName = clientName
                        };

                        obj.Add(connectionId, session);
                        return session;
                    }

                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to upsert session for session {connectionId}.", ex);
                    throw;
                }
            });
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
                _collection.TryWrite(out bool wasLockObtained, 1000, (obj) =>
                {
                    var session = obj.FirstOrDefault(o => o.Value.ProcessId == processId).Value;
                    if (session != null)
                    {
                        obj.Remove(session.ConnectionId);
                    }
                });

                if (wasLockObtained == false)
                {
                    LogManager.Warning($"Lock timeout expired while removing session. The task will be deferred to the heartbeat manager.");
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to remove sessions by processIDs.", ex);
                throw;
            }
        }

        public bool TryGetProcessByConnection(Guid connectionId, [NotNullWhen(true)] out SessionState? value)
        {
            value = _collection.Read((obj) =>
            {
                if (obj.TryGetValue(connectionId, out var value))
                {
                    return value;
                }
                return null;
            });

            if (value != null)
            {
                return true;
            }
            return false;
        }

        internal SessionState ByProcessId(ulong processId)
        {
            return _collection.Read((obj) =>
            {
                try
                {
                    var result = obj.FirstOrDefault(o => o.Value.ProcessId == processId);
                    if (result.Value != null)
                    {
                        return result.Value;
                    }
                    throw new KbSessionNotFoundException($"Session was not found: {processId}");
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to get session state by process id for process id {processId}.", ex);
                    throw;
                }
            });
        }
    }
}
