using NTDLS.Katzebase.Client.Payloads.Queries;

namespace NTDLS.Katzebase.Client.Service.Controllers
{
    public static class ProcedureController
    {
        public static KbQueryProcedureExecuteReply ExecuteProcedure(KbQueryProcedureExecute param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Query.APIHandlers.ExecuteStatementProcedure(processId, param.Procedure);
            }
            catch (Exception ex)
            {
                return new KbQueryProcedureExecuteReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }
    }
}
