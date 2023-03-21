using Katzebase.Library;
using Katzebase.Library.Payloads;
using System;
using System.Threading;
using System.Web.Http;

namespace Katzebase.Service.Controllers
{
    public class TransactionController : ApiController
    {
        [HttpGet]
        public ActionResponse Begin(Guid sessionId)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponse result = new ActionResponse();

            try
            {
                Program.Core.Transactions.Begin(processId, true);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        [HttpGet]
        public ActionResponse Commit(Guid sessionId)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponse result = new ActionResponse();

            try
            {
                Program.Core.Transactions.Commit(processId);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        [HttpGet]
        public ActionResponse Rollback(Guid sessionId)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponse result = new ActionResponse();

            try
            {
                Program.Core.Transactions.Rollback(processId);
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
