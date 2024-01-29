using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads.Queries;
using NTDLS.Katzebase.Engine;

namespace NTDLS.Katzebase.Service.APIHandlers
{
    public class ServerController
    {
        private readonly EngineCore _core;
        public ServerController(EngineCore core)
        {
            _core = core;
        }

        /// <summary>
        /// Tests the connection to the server.
        /// </summary>
        /// <param name="schema"></param>
        public KbQueryServerCloseSessionReply CloseSession(KbQueryServerCloseSession param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                _core.Sessions.CloseByProcessId(processId);

                var result = new KbQueryServerCloseSessionReply
                {
                    ProcessId = processId,
                    SessionId = param.SessionId,
                    ServerTimeUTC = DateTime.UtcNow,
                    Success = true
                };

                return result;
            }
            catch (Exception ex)
            {
                return new KbQueryServerCloseSessionReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Tests the connection to the server.
        /// </summary>
        /// <param name="schema"></param>
        public KbQueryServerTerminateProcessReply TerminateProcess(KbQueryServerTerminateProcess param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                _core.Sessions.CloseByProcessId(param.ReferencedProcessId);

                var result = new KbQueryServerTerminateProcessReply
                {
                    Success = true
                };

                return result;
            }
            catch (Exception ex)
            {
                return new KbQueryServerTerminateProcessReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }
    }
}
