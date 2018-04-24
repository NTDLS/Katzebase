using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Dokdex.Library.Payloads;

namespace Dokdex.Library.Client.Management
{
    public class Document
    {
        private DokdexClient client;

        public Document(DokdexClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Stores a document in the given schema.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public void Store(string schema, Payloads.Document document)
        {
            string url = string.Format("api/Document/{0}/{1}/Store", client.SessionId, schema);

            var postContent = new StringContent(JsonConvert.SerializeObject(document), Encoding.UTF8);

            using (var response = client.Client.PostAsync(url, postContent))
            {
                string resultText = response.Result.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<ActionResponse>(resultText);
                if (result.Success == false)
                {
                    throw new Exception(result.Message);
                }
            }
        }

        /// <summary>
        /// Deletes a document in the given schema by its Id.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public void DeleteById(string schema, Guid id)
        {
            string url = string.Format("api/Document/{0}/{1}/{2}/DeleteById", client.SessionId, schema, id);

            using (var response = client.Client.GetAsync(url))
            {
                string resultText = response.Result.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<ActionResponse>(resultText);
                if (result.Success == false)
                {
                    throw new Exception(result.Message);
                }
            }
        }

        /// <summary>
        /// Lists the doucments within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        public List<DocumentCatalogItem> Catalog(string schema)
        {
            string url = string.Format("api/Document/{0}/{1}/Catalog", client.SessionId, schema);

            using (var response = client.Client.GetAsync(url))
            {
                string resultText = response.Result.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<List<DocumentCatalogItem>>(resultText);
            }
        }
    }
}
