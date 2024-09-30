using NTDLS.Katzebase.Client.Payloads.RoundTrip;

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
        public void Create(string schema, uint pageSize = 0, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            _ = _client.Connection.Query(
                new KbQuerySchemaCreate(_client.ServerConnectionId, schema, pageSize), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        /// <summary>
        /// Creates a full schema path.
        /// </summary>
        /// <param name="schema"></param>
        public void CreateRecursive(string schema, uint pageSize = 0, TimeSpan? queryTimeout = null)
        {
            string fullSchema = string.Empty;

            foreach (var part in schema.Split(':'))
            {
                fullSchema += part;
                if (Exists(fullSchema, queryTimeout) == false)
                {
                    Create(fullSchema, pageSize, queryTimeout);
                }
                fullSchema += ':';
            }
        }

        /// <summary>
        /// Checks for the existence of a schema.
        /// </summary>
        /// <param name="schema"></param>
        public bool Exists(string schema, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQuerySchemaExists(_client.ServerConnectionId, schema), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result.Value;
        }

        /// <summary>
        /// Drops a single schema or an entire schema path.
        /// </summary>
        /// <param name="schema"></param>
        public void Drop(string schema, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            _ = _client.Connection.Query(
                new KbQuerySchemaDrop(_client.ServerConnectionId, schema), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        /// <summary>
        /// Drops a single schema or an entire schema path if it exists.
        /// </summary>
        /// <param name="schema"></param>
        public bool DropIfExists(string schema, TimeSpan? queryTimeout = null)
        {
            if (Exists(schema, queryTimeout))
            {
                Drop(schema, queryTimeout);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Lists the existing schemas within a given schema.
        /// </summary>
        /// <param name="schema"></param>
        public KbQuerySchemaListReply List(string schema, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQuerySchemaList(_client.ServerConnectionId, schema), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        /// <summary>
        /// Lists the existing root schemas.
        /// </summary>
        /// <param name="schema"></param>
        public KbQuerySchemaListReply List(TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQuerySchemaList(_client.ServerConnectionId, ":"), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }
    }
}
