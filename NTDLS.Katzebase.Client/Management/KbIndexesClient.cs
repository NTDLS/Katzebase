using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Client.Payloads.RoundTrip;

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
        /// <param name="index"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="KbAPIResponseException"></exception>
        public void Create(string schema, KbIndex index, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            _ = _client.Connection.Query(
                new KbQueryIndexCreate(_client.ServerConnectionId, schema, index), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        /// <summary>
        /// Checks for the existence of an index.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="indexName"></param>
        public bool Exists(string schema, string indexName, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryIndexExists(_client.ServerConnectionId, schema, indexName), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result.Value;
        }

        /// <summary>
        /// Gets an index from a specific schema.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="indexName"></param>
        public KbQueryIndexGetReply Get(string schema, string indexName, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryIndexGet(_client.ServerConnectionId, schema, indexName), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        /// <summary>
        /// Rebuilds a given index.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="indexName"></param>
        /// <param name="newPartitionCount"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="KbAPIResponseException"></exception>
        public void Rebuild(string schema, string indexName, uint newPartitionCount = 0, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            _ = _client.Connection.Query(
                new KbQueryIndexRebuild(_client.ServerConnectionId, schema, indexName, newPartitionCount), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        /// <summary>
        /// Deletes a given index.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="indexName"></param>
        public void Drop(string schema, string indexName, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            _ = _client.Connection.Query(
                new KbQueryIndexDrop(_client.ServerConnectionId, schema, indexName), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        /// <summary>
        /// Lists all indexes on a given schema
        /// </summary>
        /// <param name="schema"></param>
        public KbActionResponseIndexes List(string schema, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryIndexList(_client.ServerConnectionId, schema), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }
    }
}
