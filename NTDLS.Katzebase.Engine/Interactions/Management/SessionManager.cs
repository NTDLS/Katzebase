using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryProcessors;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Semaphore;
using System.Diagnostics;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to sessions.
    /// </summary>
    public class SessionManager
    {
        /// <summary>
        /// This is the username we use for creating mock sessions for system queries.
        /// These logins skip a lot of checks and are assigned administrative roles.
        /// </summary>
        internal static string BuiltInSystemUserName { get; private set; } = Guid.NewGuid().ToString();

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

        internal List<SessionState> GetExpiredSessions()
        {
            return _collection.Read((obj) =>
            {
                return obj.Where(o => o.Value.IsExpired == true || (DateTime.UtcNow - o.Value.LastCheckInTime)
                    .TotalSeconds > _core.Settings.MaxIdleConnectionSeconds).Select(o => o.Value).ToList();
            });
        }

        internal InternalSystemSessionTransaction CreateEphemeralSystemSession()
        {
            var session = _core.Sessions.CreateSession(Guid.NewGuid(), BuiltInSystemUserName, "system", true);
            var transactionReference = _core.Transactions.APIAcquire(session);
            return new InternalSystemSessionTransaction(_core, session, transactionReference);
        }

        internal SessionState CreateSession(Guid connectionId, string username, string clientName = "", bool isInternalSystemSession = false)
        {
            var roles = new List<KbRole>();

            if (username != BuiltInSystemUserName)
            {
                //Get the user roles so they can be assigned to the session.
                var userRoles = _core.Query.SystemExecuteQueryAndCommit<KbRole>("AccountRoles.kbs",
                    new
                    {
                        Username = username
                    });

                roles.AddRange(userRoles);
            }

            return _collection.Write((obj) =>
                {
                    try
                    {
                        if (obj.TryGetValue(connectionId, out SessionState? session))
                        {
                            session.LastCheckInTime = DateTime.UtcNow;
                            return session;
                        }
                        else
                        {
                            ulong processId = _nextProcessId++;

                            if (username == BuiltInSystemUserName)
                            {
                                //We add a mock administrator role because when a role with [IsAdministrator == true]
                                //  exists then all other role checks are ignored.
                                roles.Add(new KbRole(Guid.Parse(BuiltInSystemUserName), "Administrator") { IsAdministrator = true });
                            }

                            session = new SessionState(processId, connectionId,
                                username == BuiltInSystemUserName ? "system" : username,
                                clientName, roles, isInternalSystemSession);

                            obj.Add(connectionId, session);
                            return session;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"{new StackFrame(1).GetMethod()} failed for session id: [{connectionId}].", ex);
                        throw;
                    }
                });
        }

        internal SessionState GetSession(Guid connectionId)
        {
            return _collection.Read((obj) =>
            {
                if (obj.TryGetValue(connectionId, out SessionState? session))
                {
                    session.LastCheckInTime = DateTime.UtcNow;
                    return session;
                }
                throw new Exception($"Failed to find session id: [{connectionId}].");
            });
        }

        /// <summary>
        /// Kills a session and any associated transaction. This is how we kill a process.
        /// </summary>
        /// <param name="processId"></param>
        public bool TryCloseByProcessID(ulong processId)
        {
            try
            {
                if (_core.Transactions.TryCloseByProcessID(processId))
                {
                    //Once the transaction for the process has been closed, removing the process is a non-critical task.
                    // For this reason, we will "try lock" with a timeout, if we fail to remove the session now - it will be
                    // automatically retried by the HeartbeatManager.
                    var wasLockObtained = _collection.TryWrite(100, (obj) =>
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

                    return wasLockObtained;
                }
                else
                {
                    _collection.TryWrite(100, (obj) =>
                    {
                        var session = obj.FirstOrDefault(o => o.Value.ProcessId == processId).Value;
                        if (session != null)
                        {
                            //If we are unable to get here, then the heartbeat thread will clean
                            //  up the connection once the connection idle timeout is reached.
                            session.IsExpired = true;
                        }
                    });
                }
                return false;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{processId}].", ex);
                throw;
            }
        }

        public bool TryGetProcessByConnection(Guid connectionId, out ulong outProcessId)
        {
            var processId = _collection.Read((obj) =>
            {
                if (obj.TryGetValue(connectionId, out var session))
                {
                    return (ulong?)session.ProcessId;
                }
                return null;
            });

            if (processId != null)
            {
                outProcessId = (ulong)processId;
                return true;
            }

            outProcessId = 0;

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
                    throw new KbSessionNotFoundException($"Session was not found: [{processId}]");
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
