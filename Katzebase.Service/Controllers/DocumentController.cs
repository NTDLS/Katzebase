using Katzebase.Library;
using Katzebase.Library.Payloads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Web.Http;

namespace Katzebase.Service.Controllers
{
    public class DocumentController : ApiController
    {
        /// <summary>
        /// Lists the documents within a given namespace.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        //api/Namespace/List
        public List<DocumentCatalogItem> Catalog(Guid sessionId, string schema)
        {
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            var persistCatalog = Program.Core.Documents.EnumerateCatalog(processId, schema);

            List<DocumentCatalogItem> documents = new List<DocumentCatalogItem>();

            foreach (var catalogItem in persistCatalog)
            {
                documents.Add(catalogItem.ToPayload());
            }

            return documents;
        }

        public ActionResponseID Store(Guid sessionId, string schema, [FromBody]string value)
        {
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);
            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponseID result = new ActionResponseID();

            try
            {
                var content = JsonConvert.DeserializeObject<Document>(value);

                Guid newId = Guid.Empty;

                Program.Core.Documents.Store(processId, schema, content, out newId);

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
        /// Deletes a single document by its Id.
        /// </summary>
        /// <param name="schema"></param>
        [HttpGet]
        //api/Document/{Namespace}/DeleteById/{Id}
        public ActionResponse DeleteById(Guid sessionId, string schema, Guid doc)
        {
            UInt64 processId = Program.Core.Sessions.UpsertSessionId(sessionId);

            Thread.CurrentThread.Name = string.Format("API:{0}:{1}", processId, Utility.GetCurrentMethod());
            Program.Core.Log.Trace(Thread.CurrentThread.Name);

            ActionResponse result = new ActionResponse();

            try
            {
                Program.Core.Documents.DeleteById(processId, schema, doc);
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
