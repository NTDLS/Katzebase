using NTDLS.Katzebase.Client.Payloads.RoundTrip;
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
                core.Log.Write($"Failed to instantiate session API handlers.", ex);
                throw;
            }
        }

        public KbQueryServerStartSessionReply StartSession(RmContext context, KbQueryServerStartSession param)
        {
            try
            {
                var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);

#if DEBUG
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
                _core.Log.Trace(Thread.CurrentThread.Name);
#endif

                var result = new KbQueryServerStartSessionReply
                {
                    ProcessId = processId,
                    ConnectionId = context.ConnectionId,
                    ServerTimeUTC = DateTime.UtcNow,
                    Success = true
                };

                return result;
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to start session for session id {context.ConnectionId}.", ex);
                throw;
            }
        }

        public KbQueryServerCloseSessionReply CloseSession(RmContext context, KbQueryServerCloseSession param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
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

        public KbQueryServerTerminateProcessReply TerminateSession(RmContext context, KbQueryServerTerminateProcess param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            try
            {
                _core.Sessions.CloseByProcessId(param.ReferencedProcessId);

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
