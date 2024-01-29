using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads.Queries;
using NTDLS.Katzebase.Engine;

namespace NTDLS.Katzebase.Service.APIHandlers
{
    public class ProcedureController
    {
        private readonly EngineCore _core;
        public ProcedureController(EngineCore core)
        {
            _core = core;
        }

        public KbQueryProcedureExecuteReply ExecuteProcedure(KbQueryProcedureExecute param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Query.APIHandlers.ExecuteStatementProcedure(processId, param.Procedure);
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
