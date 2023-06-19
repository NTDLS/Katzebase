using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Payloads;
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
        public KbActionResponsePing Exists(Guid sessionId)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                var result = new KbActionResponsePing
                {
                    ProcessId = processId,
                    SessionId = sessionId,
                    ServerTimeUTC = DateTime.UtcNow,
                    Success = true
                };

                return result;
            }
            catch (Exception ex)
            {
                return new KbActionResponsePing
                {
                    Message = ex.Message,
                    Success = false
                };
            }
        }
    }
}
