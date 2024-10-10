using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.Engine.Scripts.Models;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to sessions.
    /// </summary>
    public class SessionAPIHandlers : IRmMessageHandler
    {
        private readonly EngineCore _core;

        public SessionAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate session API handlers.", ex);
                throw;
            }
        }

        public KbQueryServerStartSessionReply StartSession(RmContext context, KbQueryServerStartSession param)
        {
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                if (string.IsNullOrEmpty(param.Username))
                {
                    throw new Exception("No username was specified.");
                }

                using var systemSession = _core.Sessions.CreateEphemeralSystemSession();

#if DEBUG
                if (param.Username == "debug")
                {
                    systemSession.Commit();

                    LogManager.Debug($"Logged in mock user [{param.Username}].");

                    var session = _core.Sessions.CreateSession(context.ConnectionId, SessionManager.BuiltInSystemUserName, param.ClientName);

                    var result = new KbQueryServerStartSessionReply
                    {
                        ProcessId = session.ProcessId,
                        ConnectionId = context.ConnectionId,
                        ServerTimeUTC = DateTime.UtcNow
                    };
                    return result;
                }
#endif

                var account = _core.Query.InternalExecuteQuery<AccountLogin>(systemSession.Session, EmbeddedScripts.Load("AccountLogin.kbs"),
                    new
                    {
                        param.Username,
                        param.PasswordHash
                    }).FirstOrDefault();

                systemSession.Commit();

                if (account != null)
                {
                    LogManager.Debug($"Logged in user [{param.Username}].");

                    var session = _core.Sessions.CreateSession(context.ConnectionId, param.Username, param.ClientName);

                    var result = new KbQueryServerStartSessionReply
                    {
                        ProcessId = session.ProcessId,
                        ConnectionId = context.ConnectionId,
                        ServerTimeUTC = DateTime.UtcNow
                    };
                    return result;
                }

                throw new Exception("Invalid username or password.");

            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to start session for session id {context.ConnectionId}.", ex);
                throw;
            }
        }

        public KbQueryServerCloseSessionReply CloseSession(RmContext context, KbQueryServerCloseSession param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                _core.Sessions.CloseByProcessId(session.ProcessId);

                return new KbQueryServerCloseSessionReply();
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to close session for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        public KbQueryServerTerminateProcessReply TerminateSession(RmContext context, KbQueryServerTerminateProcess param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                _core.Sessions.CloseByProcessId(param.ReferencedProcessId);

                return new KbQueryServerTerminateProcessReply(); ;
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to close session for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
