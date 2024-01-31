using NTDLS.Katzebase.Client.Payloads.Queries;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to sessions.
    /// </summary>
    public class SessionAPIHandlers
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
                core.Log.Write($"Failed to instantiate session API handlers.", ex);
                throw;
            }
        }

        public KbQueryServerStartSessionReply StartSession(Guid sessionId)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(sessionId);

                var result = new KbQueryServerStartSessionReply
                {
                    ProcessId = processId,
                    SessionId = sessionId,
                    ServerTimeUTC = DateTime.UtcNow,
                    Success = true
                };

                return result;
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to start session for session id {sessionId}.", ex);
                throw;
            }
        }

        public KbQueryServerCloseSessionReply CloseSession(ulong processId)
        {
            try
            {
                _core.Sessions.CloseByProcessId(processId);

                var result = new KbQueryServerCloseSessionReply
                {
                    Success = true
                };

                return result;
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to close session for process id {processId}.", ex);
                throw;
            }
        }

        public KbQueryServerTerminateProcessReply TerminateSession(ulong processId)
        {
            try
            {
                _core.Sessions.CloseByProcessId(processId);

                var result = new KbQueryServerTerminateProcessReply
                {
                    Success = true
                };

                return result;
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to close session for process id {processId}.", ex);
                throw;
            }
        }
    }
}
