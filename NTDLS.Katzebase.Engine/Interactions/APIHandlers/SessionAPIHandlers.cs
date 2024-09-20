using NTDLS.Katzebase.Client.Payloads.RoundTrip;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Sessions;
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

                SessionState? preLogin = null;

                try
                {
                    preLogin = _core.Sessions.CreateSession(Guid.NewGuid(), param.Username, param.ClientName, true);

                    using var transactionReference = _core.Transactions.Acquire(preLogin);

                    var account = _core.Query.ExecuteQuery<Account>(preLogin,
                        $"SELECT Username, PasswordHash FROM Master:Account WHERE Username = @Username AND PasswordHash = @PasswordHash",
                        new
                        {
                            param.Username,
                            param.PasswordHash
                        }).FirstOrDefault();

                    transactionReference.Commit();

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
                finally
                {
                    if (preLogin != null)
                    {
                        _core.Sessions.CloseByProcessId(preLogin.ProcessId);
                    }
                }
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
