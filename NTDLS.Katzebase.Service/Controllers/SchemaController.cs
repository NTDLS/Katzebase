using Katzebase;
using Katzebase.Payloads;
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
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Schemas.APIHandlers.ListSchemas(processId, schema);
            }
            catch (Exception ex)
            {
                return new KbActionResponseSchemaCollection
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Creates a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/{pageSize}/Create")]
        public KbActionResponse Create(Guid sessionId, string schema, uint pageSize)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Schemas.APIHandlers.CreateSchema(processId, schema, pageSize);
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
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Schemas.APIHandlers.DoesSchemaExist(processId, schema);
            }
            catch (Exception ex)
            {
                return new KbActionResponseBoolean
                {
                    ExceptionText = ex.Message,
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
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Schemas.APIHandlers.DropSchema(processId, schema);
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
