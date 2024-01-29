using NTDLS.Katzebase.Client.Payloads.Queries;

namespace NTDLS.Katzebase.Client.Service.Controllers
{
    public static class ServerController
    {
        /// <summary>
        /// Tests the connection to the server.
        /// </summary>
        /// <param name="schema"></param>
        public static KbQueryServerCloseSessionReply CloseSession(KbQueryServerCloseSession param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                Program.Core.Sessions.CloseByProcessId(processId);

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
        public static KbQueryServerTerminateProcessReply TerminateProcess(KbQueryServerTerminateProcess param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                Program.Core.Sessions.CloseByProcessId(param.ReferencedProcessId);

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
