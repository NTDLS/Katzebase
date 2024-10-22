using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Scripts.Models;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.ReliableMessaging;
using System.Diagnostics;

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
            SessionState? session = null;

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

                using var ephemeral = _core.Sessions.CreateEphemeralSystemSession();
#if DEBUG
                if (param.Username == "debug")
                {
                    ephemeral.Commit();

                    LogManager.Debug($"Logged in mock user [{param.Username}].");

                    session = _core.Sessions.CreateSession(context.ConnectionId, SessionManager.BuiltInSystemUserName, param.ClientName);
#if DEBUG
                    Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
                    LogManager.Debug(Thread.CurrentThread.Name);
#endif
                    var apiResults = new KbQueryServerStartSessionReply
                    {
                        ProcessId = session.ProcessId,
                        ConnectionId = context.ConnectionId,
                        ServerTimeUTC = DateTime.UtcNow
                    };
                    return apiResults;
                }
#endif
                var account = ephemeral.Transaction.ExecuteQuery<AccountLogin>("AccountLogin.kbs",
                    new
                    {
                        param.Username,
                        param.PasswordHash
                    }).FirstOrDefault();

                ephemeral.Commit();

                if (account != null)
                {
                    LogManager.Debug($"Logged in user [{param.Username}].");

                    session = _core.Sessions.CreateSession(context.ConnectionId, param.Username, param.ClientName);
#if DEBUG
                    Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
                    LogManager.Debug(Thread.CurrentThread.Name);
#endif

                    var apiResults = new KbQueryServerStartSessionReply
                    {
                        ProcessId = session.ProcessId,
                        ConnectionId = context.ConnectionId,
                        ServerTimeUTC = DateTime.UtcNow
                    };
                    return apiResults;
                }

                throw new Exception("Invalid username or password.");
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session?.ProcessId}].", ex);
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
                _core.Sessions.TryCloseByProcessID(session.ProcessId);

                var apiResults = new KbQueryServerCloseSessionReply();

                return apiResults;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
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
                _core.Sessions.TryCloseByProcessID(param.ReferencedProcessId);

                var apiResult = new KbQueryServerTerminateProcessReply();

                return apiResult;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
