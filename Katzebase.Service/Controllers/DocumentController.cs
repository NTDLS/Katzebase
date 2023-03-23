using Katzebase.Library;
using Katzebase.Library.Payloads;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Katzebase.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController
    {
        /// <summary>
        /// Lists the documents within a given namespace.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/Catalog")]
        public List<KbDocumentCatalogItem> Catalog(Guid sessionId, string schema)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            var persistCatalog = Program.Core.Documents.EnumerateCatalog(processId, schema);

            List<KbDocumentCatalogItem> documents = new List<KbDocumentCatalogItem>();

            foreach (var catalogItem in persistCatalog)
            {
                documents.Add(catalogItem.ToPayload());
            }

            return documents;
        }

        [HttpPost]
        [Route("{sessionId}/{schema}/Store")]
        public KbActionResponseID Store(Guid sessionId, string schema, [FromBody] string value)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            KbActionResponseID result = new KbActionResponseID();

            try
            {
                var content = JsonConvert.DeserializeObject<KbDocument>(value);

                Utility.EnsureNotNull(content);

                Guid? newId = Guid.Empty;

                Program.Core.Documents.Store(processId, schema, content, out newId);

                Utility.EnsureNotNullOrEmpty(newId);

                result.Id = (Guid)newId;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Deletes a single document by its Id.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        [Route("{sessionId}/{schema}/{id}/DeleteById")]
        public KbActionResponse DeleteById(Guid sessionId, string schema, Guid id)
        {
            ulong processId = Program.Core.Sessions.UpsertSessionId(sessionId);

            Thread.CurrentThread.Name = Thread.CurrentThread.Name = $"API:{processId}:{Utility.GetCurrentMethod()}";
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            KbActionResponse result = new KbActionResponse();

            try
            {
                Program.Core.Documents.DeleteById(processId, schema, id);
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
