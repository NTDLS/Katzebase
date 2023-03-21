using Katzebase.Library;
using Katzebase.Library.Payloads;
using System;
using System.Threading;
using System.Web.Http;

namespace Katzebase.Service.Controllers
{
    public class SchemaController : ApiController
    {
        /// <summary>
        /// Lists the existing namespaces within a given namespace.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        //api/Namespace/List
        public ActionResponseSchemas List(Guid sessionId, string schema)
        {
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponseSchemas result = new ActionResponseSchemas();

            try
            {
                var persistSchemas = Program.Core.Schemas.GetList(processId, schema);

                foreach (var persistSchema in persistSchemas)
                {
                    result.Add(persistSchema.ToPayload());
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Creates a single namespace or an entire namespace path.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        //api/Namespace/{Namespace}/Create
        public ActionResponse Create(Guid sessionId, string schema)
        {
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponse result = new ActionResponse();

            try
            {
                Program.Core.Schemas.Create(processId, schema);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Checks for the existence of a schema.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        //api/Namespace/{Namespace}/Exists
        public ActionResponseBoolean Exists(Guid sessionId, string schema)
        {
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponseBoolean result = new ActionResponseBoolean();

            try
            {
                result.Value = Program.Core.Schemas.Exists(processId, schema);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Drops a single namespace or an entire namespace path.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        //api/Namespace/{Namespace}/Drop
        public ActionResponse Drop(Guid sessionId, string schema)
        {
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponse result = new ActionResponse();

            try
            {
                Program.Core.Schemas.Drop(processId, schema);
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
