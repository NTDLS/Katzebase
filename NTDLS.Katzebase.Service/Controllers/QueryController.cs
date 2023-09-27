using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NTDLS.Katzebase.Client.Payloads;

namespace NTDLS.Katzebase.Client.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController
    {
        [HttpPost]
        [Route("{sessionId}/ExplainQuery")]
        public KbQueryResultCollection ExplainQuery(Guid sessionId, [FromBody] string value)
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
                return new KbQueryResultCollection
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        [HttpPost]
        [Route("{sessionId}/ExecuteQuery")]
        public KbQueryResultCollection ExecuteQuery(Guid sessionId, [FromBody] string value)
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
                return new KbQueryResultCollection
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        [HttpPost]
        [Route("{sessionId}/ExecuteQueries")]
        public KbQueryResultCollection ExecuteQueries(Guid sessionId, [FromBody] string values)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                var statements = JsonConvert.DeserializeObject<List<string>>(values);
                KbUtility.EnsureNotNull(statements);
                return Program.Core.Query.APIHandlers.ExecuteStatementQueries(processId, statements);
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

        [HttpPost]
        [Route("{sessionId}/ExecuteNonQuery")]
        public KbActionResponseCollection ExecuteNonQuery(Guid sessionId, [FromBody] string value)
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
                return new KbActionResponseCollection
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }
    }
}
