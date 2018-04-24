using System;
using System.Collections.Generic;
using System.Threading;
using System.Web.Http;
using Dokdex.Library.Payloads;
using Newtonsoft.Json;
using Dokdex.Library;

namespace Dokdex.Service.Controllers
{
    public class QueryController : ApiController
    {
        public ActionResponse Execute(Guid sessionId, [FromBody]string value)
        {
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
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
