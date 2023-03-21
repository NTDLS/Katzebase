using Katzebase.Library;
using Katzebase.Library.Payloads;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Web.Http;

namespace Katzebase.Service.Controllers
{
    public class QueryController : ApiController
    {
        public ActionResponse Execute(Guid sessionId, [FromBody] string value)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponseID result = new ActionResponseID();

            try
            {
                var statement = JsonConvert.DeserializeObject<string>(value);

                Program.Core.Query.Execute(processId, statement);

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
