using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Payloads;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Katzebase.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IndexesController
    {
        [HttpPost]
        [Route("{sessionId}/{schema}/Create")]
        public KbActionResponseGuid Create(Guid sessionId, string schema, [FromBody] string value)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                var content = JsonConvert.DeserializeObject<KbIndex>(value);
                KbUtility.EnsureNotNull(content);

                return Program.Core.Indexes.APIHandlers.CreateIndex(processId, schema, content);
            }
            catch (Exception ex)
            {
                return new KbActionResponseGuid
                {
                    Message = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Rebuilds a single index.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/{name}/Rebuild")]
        public KbActionResponse Rebuild(Guid sessionId, string schema, string name)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Indexes.APIHandlers.RebuildIndex(processId, schema, name);
            }
            catch (Exception ex)
            {
                return new KbActionResponseGuid
                {
                    Message = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Drops a single index.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/{name}/Drop")]
        public KbActionResponse Drop(Guid sessionId, string schema, string name)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Indexes.APIHandlers.DropIndex(processId, schema, name);
            }
            catch (Exception ex)
            {
                return new KbActionResponseGuid
                {
                    Message = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Checks for the existence of an index.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/{name}/Exists")]
        public KbActionResponseBoolean Exists(Guid sessionId, string schema, string name)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Indexes.APIHandlers.DoesIndexExist(processId, schema, name);
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
        /// Lists the existing indexes within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/List")]
        public KbActionResponseIndexes List(Guid sessionId, string schema)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Indexes.APIHandlers.ListIndexes(processId, schema);
            }
            catch (Exception ex)
            {
                return new KbActionResponseIndexes
                {
                    Message = ex.Message,
                    Success = false
                };
            }
        }
    }
}
