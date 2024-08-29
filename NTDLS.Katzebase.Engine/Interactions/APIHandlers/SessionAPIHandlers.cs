using NTDLS.Katzebase.Client.Payloads.RoundTrip;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Query.Searchers;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.ReliableMessaging;
using static System.Formats.Asn1.AsnWriter;

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
                Management.LogManager.Error($"Failed to instantiate session API handlers.", ex);
                throw;
            }
        }

        public KbQueryServerStartSessionReply StartSession(RmContext context, KbQueryServerStartSession param)
        {
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{param.GetType().Name}";
            Management.LogManager.Debug(Thread.CurrentThread.Name);
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
                    preLogin = _core.Sessions.CreateSession(Guid.NewGuid(), param.Username, param.ClientName);

                    using var transactionReference = _core.Transactions.Acquire(preLogin);
                    var accounts = StaticSearcherMethods.ListSchemaDocuments(_core, transactionReference.Transaction, "Master:Account", -1);
                    transactionReference.Commit();

                    int usernameIndex = accounts.IndexOf("username");
                    int passwordHashIndex = accounts.IndexOf("passwordHash");

                    //Loop through all of the users, we we find a match then check its password hash. Otherwise throw an exception.
                    foreach (var row in accounts.Rows)
                    {
                        if (accounts.RowValue(row, usernameIndex)?.Equals(param.Username, StringComparison.InvariantCultureIgnoreCase) == true)
                        {
                            if (accounts.RowValue(row, passwordHashIndex)?.Equals(param.PasswordHash, StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                LogManager.Information($"Logged in user [{usernameIndex}].");

                                //If and only if we find the correct username and password do we create the session.
                                var session = _core.Sessions.CreateSession(context.ConnectionId, param.Username, param.ClientName);

                                var result = new KbQueryServerStartSessionReply
                                {
                                    ProcessId = session.ProcessId,
                                    ConnectionId = context.ConnectionId,
                                    ServerTimeUTC = DateTime.UtcNow
                                };

                                return result;
                            }
                            LogManager.Information($"Invalid password for user [{param.Username}].");
                            throw new Exception("Invalid username or password."); 
                        }
                    }

                    LogManager.Information($"Username not found [{param.Username}].");
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
                Management.LogManager.Error($"Failed to start session for session id {context.ConnectionId}.", ex);
                throw;
            }
        }

        public KbQueryServerCloseSessionReply CloseSession(RmContext context, KbQueryServerCloseSession param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            Management.LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                _core.Sessions.CloseByProcessId(session.ProcessId);

                return new KbQueryServerCloseSessionReply();
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to close session for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        public KbQueryServerTerminateProcessReply TerminateSession(RmContext context, KbQueryServerTerminateProcess param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            Management.LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                _core.Sessions.CloseByProcessId(param.ReferencedProcessId);

                return new KbQueryServerTerminateProcessReply(); ;
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to close session for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
