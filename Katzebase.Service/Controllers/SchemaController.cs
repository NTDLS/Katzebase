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
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Schemas.APIHandlers.ListSchemas(processId, schema);
            }
            catch (Exception ex)
            {
                return new KbActionResponseSchemaCollection
                {
                    Message = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Creates a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/Create")]
        public KbActionResponse Create(Guid sessionId, string schema)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Schemas.APIHandlers.CreateSchema(processId, schema);
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

        /// <summary>
        /// Checks for the existence of a schema.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/Exists")]
        public KbActionResponseBoolean Exists(Guid sessionId, string schema)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Schemas.APIHandlers.DoesSchemaExist(processId, schema);
            }
            catch (Exception ex)
            {
                return new KbActionResponseBoolean
                {
                    Message = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Drops a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/Drop")]
        public KbActionResponse Drop(Guid sessionId, string schema)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Schemas.APIHandlers.DropSchema(processId, schema);
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
