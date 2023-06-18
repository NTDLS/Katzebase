using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Payloads;
using Microsoft.AspNetCore.Mvc;

namespace Katzebase.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SchemaController
    {
        /// <summary>
        /// Lists the existing schemas within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/List")]
        public KbActionResponseSchemaCollection List(Guid sessionId, string schema)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            var result = new KbActionResponseSchemaCollection();

            try
            {
                result = Program.Core.Schemas.APIHandlers.ListSchemas(processId, schema);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Creates a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/Create")]
        public KbActionResponse Create(Guid sessionId, string schema)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            var result = new KbActionResponse();

            try
            {
                Program.Core.Schemas.APIHandlers.CreateSchema(processId, schema);
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
        [Route("{sessionId}/{schema}/Exists")]
        public KbActionResponseBoolean Exists(Guid sessionId, string schema)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            var result = new KbActionResponseBoolean();

            try
            {
                result.Value = Program.Core.Schemas.APIHandlers.DoesSchemaExist(processId, schema);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Drops a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/Drop")]
        public KbActionResponse Drop(Guid sessionId, string schema)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            var result = new KbActionResponse();

            try
            {
                Program.Core.Schemas.APIHandlers.DropSchema(processId, schema);
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
