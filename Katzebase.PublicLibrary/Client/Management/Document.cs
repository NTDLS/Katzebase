using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json;
using System.Text;

namespace Katzebase.PublicLibrary.Client.Management
{
    public class Document
    {
        private readonly KatzebaseClient client;

        public Document(KatzebaseClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Stores a document in the given schema.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public void Store(string schema, Payloads.KbDocument document)
        {
            string url = $"api/Document/{client.SessionId}/{schema}/Store";

            var postContent = new StringContent(JsonConvert.SerializeObject(document), Encoding.UTF8, "text/plain");

            using var response = client.Connection.PostAsync(url, postContent);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.Message);
            }
        }

        /// <summary>
        /// Deletes a document in the given schema by its Id.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public void DeleteById(string schema, Guid id)
        {
            string url = $"api/Document/{client.SessionId}/{schema}/{id}/DeleteById";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.Message);
            }
        }

        /// <summary>
        /// Lists the documents within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        public List<KbDocumentCatalogItem> Catalog(string schema)
        {
            string url = $"api/Document/{client.SessionId}/{schema}/Catalog";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<List<KbDocumentCatalogItem>>(resultText) ?? new List<KbDocumentCatalogItem>();
        }

        /// <summary>
        /// Lists the documents within a given schema with their values.
        /// </summary>
        /// <param name="schema"></param>
        public KbQueryResult List(string schema, int count = -1)
        {
            string url = $"api/Document/{client.SessionId}/{schema}/List/{count}";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<KbQueryResult>(resultText) ?? new KbQueryResult();
        }

        /// <summary>
        /// Samples the documents within a given schema with their values.
        /// </summary>
        /// <param name="schema"></param>
        public KbQueryResult Sample(string schema, int count)
        {
            string url = $"api/Document/{client.SessionId}/{schema}/Sample/{count}";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<KbQueryResult>(resultText) ?? new KbQueryResult();
        }
    }
}
