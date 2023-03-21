using Dokdex.Library;
using Dokdex.Library.Payloads;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Web.Http;

namespace Dokdex.Service.Controllers
{
    public class IndexesController : ApiController
    {
        public ActionResponseID Create(Guid sessionId, string schema, [FromBody]string value)
        {
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponseID result = new ActionResponseID();

            try
            {
                var content = JsonConvert.DeserializeObject<Index>(value);

                Guid newId = Guid.Empty;

                Program.Core.Indexes.Create(processId, schema, content, out newId);

                result.Id = newId;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Rebuilds a single index.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        public ActionResponse Rebuild(Guid sessionId, string schema, string byName)
        {
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponse result = new ActionResponseBoolean();

            try
            {
                Program.Core.Indexes.Exists(processId, schema, byName);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Checks for the existence of an index.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        public ActionResponseBoolean Exists(Guid sessionId, string schema, string byName)
        {
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponseBoolean result = new ActionResponseBoolean();

            try
            {
                result.Value = Program.Core.Indexes.Exists(processId, schema, byName);
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
