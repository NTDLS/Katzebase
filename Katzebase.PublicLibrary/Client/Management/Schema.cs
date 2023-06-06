using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json;

namespace Katzebase.PublicLibrary.Client.Management
{
    public class Schema
    {
        private readonly KatzebaseClient client;

        public Indexes Indexes { get; set; }

        public Schema(KatzebaseClient client)
        {
            this.client = client;
            this.Indexes = new Indexes(client);
        }

        /// <summary>
        /// Creates a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        public void Create(string schema)
        {
            string url = $"api/Schema/{client.SessionId}/{schema}/Create";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.Message);
            }
        }

        /// <summary>
        /// Checks for the existence of a schema.
        /// </summary>
        /// <param name="schema"></param>
        public bool Exists(string schema)
        {
            string url = $"api/Schema/{client.SessionId}/{schema}/Exists";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponseBoolean>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.Message);
            }

            return result.Value;
        }

        /// <summary>
        /// Drops a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        public void Drop(string schema)
        {
            string url = $"api/Schema/{client.SessionId}/{schema}/Drop";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.Message);
            }
        }

        /// <summary>
        /// Lists the existing schemas within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        public KbActionResponseSchemas List(string schema)
        {
            string url = $"api/Schema/{client.SessionId}/{schema}/List";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<KbActionResponseSchemas>(resultText) ?? new KbActionResponseSchemas();
        }

        /// <summary>
        /// Lists the existing root schemas.
        /// </summary>
        /// <param name="schema"></param>
        public KbActionResponseSchemas List()
        {
            string url = $"api/Schema/{client.SessionId}/:/List";

            using var response = client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<KbActionResponseSchemas>(resultText) ?? new KbActionResponseSchemas();
        }
    }
}
