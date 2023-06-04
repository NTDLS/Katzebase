using Katzebase.Library;
using Katzebase.Library.Payloads;
using Microsoft.AspNetCore.Mvc;

namespace Katzebase.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServerController
    {
        /// <summary>
        /// Tests the connection to the server.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/Ping")]
        public KbActionResponseBoolean Exists(Guid sessionId)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            KbActionResponseBoolean result = new KbActionResponseBoolean();

            try
            {
                result.Value = true;
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
