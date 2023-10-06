using Newtonsoft.Json;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;

namespace NTDLS.Katzebase.Client.Management
{
    public class KbSchemaClient
    {
        private readonly KbClient _client;

        public KbIndexesClient Indexes { get; set; }

        public KbSchemaClient(KbClient client)
        {
            _client = client;
            Indexes = new KbIndexesClient(client);
        }

        /// <summary>
        /// Creates a single schema.
        /// </summary>
        /// <param name="schema"></param>
        public void Create(string schema, int pageSize = 0)
        {
            string url = $"api/Schema/{_client.SessionId}/{schema}/{pageSize}/Create";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
        }

        /// <summary>
        /// Creates a full schema path
        /// </summary>
        /// <param name="schema"></param>
        public void CreateFullSchema(string schema, int pageSize = 0)
        {
            string fullSchema = string.Empty;

            foreach (var part in schema.Split(':'))
            {
                fullSchema += part;
                if (Exists(fullSchema) == false)
                {
                    Create(fullSchema, pageSize);
                }
                fullSchema += ':';
            }
        }

        /// <summary>
        /// Checks for the existence of a schema.
        /// </summary>
        /// <param name="schema"></param>
        public bool Exists(string schema)
        {
            string url = $"api/Schema/{_client.SessionId}/{schema}/Exists";

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
        /// Drops a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        public void Drop(string schema)
        {
            string url = $"api/Schema/{_client.SessionId}/{schema}/Drop";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponse>(resultText);
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
        }

        /// <summary>
        /// Drops a single schema or an entire schema path if it exists.
        /// </summary>
        /// <param name="schema"></param>
        public bool DropIfExists(string schema)
        {
            if (Exists(schema))
            {
                Drop(schema);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Lists the existing schemas within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        public KbActionResponseSchemaCollection List(string schema)
        {
            string url = $"api/Schema/{_client.SessionId}/{schema}/List";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponseSchemaCollection>(resultText) ?? new KbActionResponseSchemaCollection();
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
            return result;
        }

        /// <summary>
        /// Lists the existing root schemas.
        /// </summary>
        /// <param name="schema"></param>
        public KbActionResponseSchemaCollection List()
        {
            string url = $"api/Schema/{_client.SessionId}/:/List";

            using var response = _client.Connection.GetAsync(url);
            string resultText = response.Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<KbActionResponseSchemaCollection>(resultText) ?? new KbActionResponseSchemaCollection();
            if (result == null || result.Success == false)
            {
                throw new KbAPIResponseException(result == null ? "Invalid response" : result.ExceptionText);
            }
            return result;
        }
    }
}
