using Katzebase;
using Katzebase.Payloads;
using Microsoft.AspNetCore.Mvc;

namespace Katzebase.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController
    {
        [HttpGet]
        [Route("{sessionId}/Begin")]
        public KbActionResponse Begin(Guid sessionId)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                Program.Core.Transactions.APIHandlers.Begin(processId);
                return new KbActionResponse { Success = true };
            }
            catch (Exception ex)
            {
                return new KbActionResponse
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }

        }

        [HttpGet]
        [Route("{sessionId}/Commit")]
        public KbActionResponse Commit(Guid sessionId)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                Program.Core.Transactions.APIHandlers.Commit(processId);
                return new KbActionResponse { Success = true };
            }
            catch (Exception ex)
            {
                return new KbActionResponse
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }

        }

        [HttpGet]
        [Route("{sessionId}/Rollback")]
        public KbActionResponse Rollback(Guid sessionId)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                Program.Core.Transactions.APIHandlers.Rollback(processId);
                return new KbActionResponse { Success = true };
            }
            catch (Exception ex)
            {
                return new KbActionResponse
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }
    }
}
