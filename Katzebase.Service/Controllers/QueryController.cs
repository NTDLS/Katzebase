using Katzebase.Library;
using Katzebase.Library.Payloads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace Katzebase.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController
    {
        [HttpGet]
        [Route("{sessionId}/Execute")]
        public KbActionResponse Execute(Guid sessionId, [FromBody] string value)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            KbActionResponseID result = new KbActionResponseID();

            try
            {
                var statement = JsonConvert.DeserializeObject<string>(value);

                if (statement == null)
                    throw new Exception("Statement cannot be null.");

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
