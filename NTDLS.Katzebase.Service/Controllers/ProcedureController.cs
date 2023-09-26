using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NTDLS.Katzebase.Payloads;

namespace NTDLS.Katzebase.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcedureController
    {
        [HttpPost]
        [Route("{sessionId}/ExecuteProcedure")]
        public KbQueryResultCollection ExecuteProcedure(Guid sessionId, [FromBody] string value)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                var procedure = JsonConvert.DeserializeObject<KbProcedure>(value);
                KbUtility.EnsureNotNull(procedure);
                return Program.Core.Query.APIHandlers.ExecuteStatementProcedure(processId, procedure);
            }
            catch (Exception ex)
            {
                return new KbQueryResultCollection
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }
    }
}
