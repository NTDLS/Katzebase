using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Payloads;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Katzebase.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController
    {
        [HttpPost]
        [Route("{sessionId}/ExplainQuery")]
        public KbQueryResult ExplainQuery(Guid sessionId, [FromBody] string value)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                var statement = JsonConvert.DeserializeObject<string>(value);
                KbUtility.EnsureNotNull(statement);
                return Program.Core.Query.APIHandlers.ExecuteStatementExplain(processId, statement);
            }
            catch (Exception ex)
            {
                return new KbQueryResult
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        [HttpPost]
        [Route("{sessionId}/ExecuteQuery")]
        public KbQueryResult ExecuteQuery(Guid sessionId, [FromBody] string value)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                var statement = JsonConvert.DeserializeObject<string>(value);
                KbUtility.EnsureNotNull(statement);
                return Program.Core.Query.APIHandlers.ExecuteStatementQuery(processId, statement);
            }
            catch (Exception ex)
            {
                return new KbQueryResult
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        [HttpPost]
        [Route("{sessionId}/ExecuteNonQuery")]
        public KbActionResponse ExecuteNonQuery(Guid sessionId, [FromBody] string value)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                var statement = JsonConvert.DeserializeObject<string>(value);
                KbUtility.EnsureNotNull(statement);
                return Program.Core.Query.APIHandlers.ExecuteStatementNonQuery(processId, statement);
            }
            catch (Exception ex)
            {
                return new KbActionResponseBoolean
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }
    }
}
