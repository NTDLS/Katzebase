using Katzebase.Library;
using Katzebase.Library.Payloads;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Katzebase.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController
    {
        [HttpPost]
        [Route("{sessionId}/ExecuteQuery")]
        public KbQueryResult ExecuteQuery(Guid sessionId, [FromBody] string value)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            var result = new KbQueryResult();

            try
            {
                var statement = JsonConvert.DeserializeObject<string>(value);
                Utility.EnsureNotNull(statement);
                result = Program.Core.Query.ExecuteQuery(processId, statement);

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }
    }
}
