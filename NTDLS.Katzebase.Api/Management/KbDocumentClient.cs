using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads;

namespace NTDLS.Katzebase.Api.Management
{
    public class KbDocumentClient
    {
        private readonly KbClient _client;

        public KbDocumentClient(KbClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Stores a document in the given schema.
        /// </summary>
        public void Store(string schema, KbDocument document, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            _ = _client.Connection.Query(
                new KbQueryDocumentStore(_client.ServerConnectionId, schema, document), (TimeSpan)queryTimeout);
        }

        /// <summary>
        /// Stores a document in the given schema.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="document"></param>
        public void Store(string schema, object document, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            _ = _client.Connection.Query(
                new KbQueryDocumentStore(_client.ServerConnectionId, schema, new KbDocument(document)), (TimeSpan)queryTimeout);
        }

        /// <summary>
        /// Stores a batch of documents in the given schema using a single round trip to the server.
        /// The batch is atomic: if any document fails to store (e.g. a unique key violation) then none are stored.
        /// </summary>
        /// <returns>The ids of the stored documents, in the order they were given.</returns>
        public List<uint> StoreMany(string schema, IEnumerable<KbDocument> documents, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            var documentList = documents as List<KbDocument> ?? documents.ToList();
            if (documentList.Count == 0)
            {
                return new List<uint>();
            }

            return _client.Connection.Query(
                new KbQueryDocumentStoreMany(_client.ServerConnectionId, schema, documentList), (TimeSpan)queryTimeout).DocumentIds;
        }

        /// <summary>
        /// Stores a batch of objects (serialized to JSON) in the given schema using a single round trip to the server.
        /// The batch is atomic: if any document fails to store (e.g. a unique key violation) then none are stored.
        /// </summary>
        /// <returns>The ids of the stored documents, in the order they were given.</returns>
        public List<uint> StoreMany(string schema, IEnumerable<object> documents, TimeSpan? queryTimeout = null)
            => StoreMany(schema, documents.Select(o => o as KbDocument ?? new KbDocument(o)), queryTimeout);

        /// <summary>
        /// Lists the documents within a given schema with their values.
        /// </summary>
        /// <param name="schema"></param>
        public KbQueryDocumentListReply List(string schema, int count = -1, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryDocumentList(_client.ServerConnectionId, schema, count), (TimeSpan)queryTimeout);
        }

        /// <summary>
        /// Samples the documents within a given schema with their values.
        /// </summary>
        /// <param name="schema"></param>
        public KbQueryDocumentSampleReply Sample(string schema, int count, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryDocumentSample(_client.ServerConnectionId, schema, count), (TimeSpan)queryTimeout);
        }
    }
}
