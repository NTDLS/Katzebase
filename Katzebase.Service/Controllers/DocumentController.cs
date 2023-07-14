using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Payloads;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Katzebase.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController
    {
        /// <summary>
        /// Lists the documents within a given schema with their values.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/List/{count}")]
        public KbQueryResult List(Guid sessionId, string schema, int count)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Documents.APIHandlers.ListDocuments(processId, schema, count);
            }
            catch (Exception ex)
            {
                return new KbQueryResult
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Samples the documents within a given schema with their values.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/Sample/{count}")]
        public KbQueryResult Sample(Guid sessionId, string schema, int count)
        {
            try
            {
                ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Documents.APIHandlers.DocumentSample(processId, schema, count);
            }
            catch (Exception ex)
            {
                return new KbQueryResult
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Lists the documents within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/Catalog")]
        public KbDocumentCatalogCollection Catalog(Guid sessionId, string schema)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Documents.APIHandlers.DocumentCatalog(processId, schema);
            }
            catch (Exception ex)
            {
                return new KbDocumentCatalogCollection
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        [HttpPost]
        [Route("{sessionId}/{schema}/Store")]
        public KbActionResponseUInt Store(Guid sessionId, string schema, [FromBody] string value)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                var content = JsonConvert.DeserializeObject<KbDocument>(value);
                KbUtility.EnsureNotNull(content);
                return Program.Core.Documents.APIHandlers.StoreDocument(processId, schema, content);
            }
            catch (Exception ex)
            {
                return new KbActionResponseUInt
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        /// <summary>
        /// Deletes a single document by its Id.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/{id}/DeleteById")]
        public KbActionResponse DeleteById(Guid sessionId, string schema, uint id)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(sessionId);
                Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Documents.APIHandlers.DeleteDocumentById(processId, schema, id);
            }
            catch (Exception ex)
            {
                return new KbActionResponseUInt
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }
    }
}
