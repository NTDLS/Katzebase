using Dokdex.Library;
using Dokdex.Library.Payloads;
using System;
using System.Threading;
using System.Web.Http;

namespace Dokdex.Service.Controllers
{
    public class TransactionController : ApiController
    {
        [HttpGet]
        public ActionResponse Begin(Guid sessionId)
        {
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
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
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
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
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
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
