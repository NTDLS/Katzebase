using Newtonsoft.Json;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using System.Text;

namespace NTDLS.Katzebase.Client.Management
{
    public class KbIndexesClient
    {
        private readonly KbClient _client;

        public KbIndexesClient(KbClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Creates an index on the given schema.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public void Create(string schema, KbIndex index)
        {
            string url = $"api/Indexes/{_client.SessionId}/{schema}/Create";

            var postContent = new StringContent(JsonConvert.SerializeObject(index), Encoding.UTF8, "text/plain");

            using var response = _client.Connection.PostAsync(url, postContent);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
        }

        /// <summary>
        /// Creates a unique index on the given schema.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public void Create(string schema, KbUniqueKey index)
        {
            string url = $"api/Indexes/{_client.SessionId}/{schema}/Create";

            var postContent = new StringContent(JsonConvert.SerializeObject(index), Encoding.UTF8, "text/plain");

            using var response = _client.Connection.PostAsync(url, postContent);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
        }

        /// <summary>
        /// Checks for the existence of an index.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public bool Exists(string schema, string indexName)
        {
            string url = $"api/Indexes/{_client.SessionId}/{schema}/{indexName}/Exists";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponseBoolean>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }

            return result.Value;
        }

        /// <summary>
        /// Gets an index from a specific schema.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public KbActionResponseIndex Get(string schema, string indexName)
        {
            string url = $"api/Indexes/{_client.SessionId}/{schema}/{indexName}/Get";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponseIndex>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }

            return result;
        }

        /// <summary>
        /// Rebuilds a given index.
        /// </summary>
        public bool Rebuild(string schema, string indexName, int newPartitionCount = 0)
        {
            string url = $"api/Indexes/{_client.SessionId}/{schema}/{indexName}/{newPartitionCount}/Rebuild";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponseBoolean>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }

            return result.Value;
        }

        /// <summary>
        /// Deletes a given index.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public bool Drop(string schema, string indexName)
        {
            string url = $"api/Indexes/{_client.SessionId}/{schema}/{indexName}/Drop";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponseBoolean>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }

            return result.Value;
        }

        /// <summary>
        /// Lists all indexes on a given schema
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public KbActionResponseIndexes List(string schema)
        {
            string url = $"api/Indexes/{_client.SessionId}/{schema}/List";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponseIndexes>(resultText) ?? new KbActionResponseIndexes();
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
            return result;
        }
    }
}
