using Katzebase.Library;
using Katzebase.Library.Payloads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace Katzebase.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IndexesController
    {
        [HttpGet]
        [Route("{sessionId}/{schema}/Create")]
        public KbActionResponseID Create(Guid sessionId, string schema, [FromBody] string value)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            KbActionResponseID result = new KbActionResponseID();

            try
            {
                var content = JsonConvert.DeserializeObject<Library.Payloads.KbIndex>(value);

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
        [Route("{sessionId}/{schema}/{name}/Rebuild")]
        public KbActionResponse Rebuild(Guid sessionId, string schema, string name)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            KbActionResponse result = new KbActionResponseBoolean();

            try
            {
                Program.Core.Indexes.Exists(processId, schema, name);
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
        [Route("{sessionId}/{schema}/{name}/Exists")]
        public KbActionResponseBoolean Exists(Guid sessionId, string schema, string name)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            KbActionResponseBoolean result = new KbActionResponseBoolean();

            try
            {
                result.Value = Program.Core.Indexes.Exists(processId, schema, name);
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
