using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads;

namespace NTDLS.Katzebase.Api.Management
{
    /// <summary>
    /// Direct document access by id, without writing statements.
    ///
    /// Documents can be given as any object (serialized to JSON), as a string containing a JSON object, or as a <see cref="KbDocument"/>.
    /// Each call is atomic; to combine several calls into one atomic unit, use a transaction (<see cref="KbTransactionClient"/>).
    /// </summary>
    public class KbDocumentClient
    {
        private readonly KbClient _client;

        internal KbDocumentClient(KbClient client)
        {
            _client = client;
        }

        private static KbDocument ToDocument(object document)
            => document as KbDocument ?? new KbDocument(document);

        #region Store.

        /// <summary>
        /// Stores a new document and returns its id.
        /// </summary>
        public uint Store(string schema, object document, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryDocumentStore(_client.ServerConnectionId, schema, ToDocument(document)), queryTimeout).Value;

        /// <summary>
        /// Stores a new document and returns its id.
        /// </summary>
        public async Task<uint> StoreAsync(string schema, object document, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => (await _client.SendAsync(new KbQueryDocumentStore(_client.ServerConnectionId, schema, ToDocument(document)), queryTimeout, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Stores a batch of documents using a single round trip to the server, returning their ids in the order they were given.
        /// The batch is atomic: if any document fails to store (e.g. a unique key violation) then none are stored.
        /// Batches of around 1,000 documents give the best throughput.
        /// </summary>
        public List<uint> StoreMany(string schema, IEnumerable<object> documents, TimeSpan? queryTimeout = null)
        {
            var batch = documents.Select(ToDocument).ToList();
            if (batch.Count == 0)
            {
                return new List<uint>();
            }
            return _client.Send(new KbQueryDocumentStoreMany(_client.ServerConnectionId, schema, batch), queryTimeout).DocumentIds;
        }

        /// <summary>
        /// Stores a batch of documents using a single round trip to the server. See <see cref="StoreMany"/>.
        /// </summary>
        public async Task<List<uint>> StoreManyAsync(string schema, IEnumerable<object> documents,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
        {
            var batch = documents.Select(ToDocument).ToList();
            if (batch.Count == 0)
            {
                return new List<uint>();
            }
            return (await _client.SendAsync(new KbQueryDocumentStoreMany(_client.ServerConnectionId, schema, batch), queryTimeout, cancellationToken).ConfigureAwait(false)).DocumentIds;
        }

        #endregion

        #region Get.

        /// <summary>
        /// Gets a document by id, or null if it does not exist.
        /// </summary>
        public KbDocument? Get(string schema, uint documentId, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryDocumentGet(_client.ServerConnectionId, schema, documentId), queryTimeout).Document;

        /// <summary>
        /// Gets a document by id, or null if it does not exist.
        /// </summary>
        public async Task<KbDocument?> GetAsync(string schema, uint documentId, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => (await _client.SendAsync(new KbQueryDocumentGet(_client.ServerConnectionId, schema, documentId), queryTimeout, cancellationToken).ConfigureAwait(false)).Document;

        /// <summary>
        /// Gets a document by id, deserialized to T, or default if it does not exist.
        /// </summary>
        public T? Get<T>(string schema, uint documentId, TimeSpan? queryTimeout = null)
        {
            var document = Get(schema, documentId, queryTimeout);
            return document == null ? default : document.Deserialize<T>();
        }

        /// <summary>
        /// Gets a document by id, deserialized to T, or default if it does not exist.
        /// </summary>
        public async Task<T?> GetAsync<T>(string schema, uint documentId, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
        {
            var document = await GetAsync(schema, documentId, queryTimeout, cancellationToken).ConfigureAwait(false);
            return document == null ? default : document.Deserialize<T>();
        }

        #endregion

        #region Replace.

        /// <summary>
        /// Replaces the entire content of an existing document, keeping its id (fields not in the new content are removed).
        /// Throws <see cref="Exceptions.KbObjectNotFoundException"/> if the document does not exist.
        /// </summary>
        public void Replace(string schema, uint documentId, object document, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryDocumentReplace(_client.ServerConnectionId, schema, documentId, ToDocument(document)), queryTimeout);

        /// <summary>
        /// Replaces the entire content of an existing document, keeping its id. See <see cref="Replace"/>.
        /// </summary>
        public Task ReplaceAsync(string schema, uint documentId, object document, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryDocumentReplace(_client.ServerConnectionId, schema, documentId, ToDocument(document)), queryTimeout, cancellationToken);

        #endregion

        #region Delete.

        /// <summary>
        /// Deletes a document by id, returning false if it did not exist.
        /// </summary>
        public bool Delete(string schema, uint documentId, TimeSpan? queryTimeout = null)
            => DeleteMany(schema, [documentId], queryTimeout) == 1;

        /// <summary>
        /// Deletes a document by id, returning false if it did not exist.
        /// </summary>
        public async Task<bool> DeleteAsync(string schema, uint documentId, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => await DeleteManyAsync(schema, [documentId], queryTimeout, cancellationToken).ConfigureAwait(false) == 1;

        /// <summary>
        /// Deletes documents by id, atomically, returning the number that existed and were deleted.
        /// </summary>
        public int DeleteMany(string schema, IEnumerable<uint> documentIds, TimeSpan? queryTimeout = null)
        {
            var ids = documentIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return 0;
            }
            return _client.Send(new KbQueryDocumentDelete(_client.ServerConnectionId, schema, ids), queryTimeout).DeletedCount;
        }

        /// <summary>
        /// Deletes documents by id, atomically, returning the number that existed and were deleted.
        /// </summary>
        public async Task<int> DeleteManyAsync(string schema, IEnumerable<uint> documentIds, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
        {
            var ids = documentIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return 0;
            }
            return (await _client.SendAsync(new KbQueryDocumentDelete(_client.ServerConnectionId, schema, ids), queryTimeout, cancellationToken).ConfigureAwait(false)).DeletedCount;
        }

        #endregion

        #region List / sample.

        /// <summary>
        /// Lists the documents in a schema (all of them when count is negative) as a result set of their fields.
        /// </summary>
        public KbQueryDocumentListReply List(string schema, int count = -1, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryDocumentList(_client.ServerConnectionId, schema, count), queryTimeout);

        /// <summary>
        /// Lists the documents in a schema (all of them when count is negative) as a result set of their fields.
        /// </summary>
        public Task<KbQueryDocumentListReply> ListAsync(string schema, int count = -1, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryDocumentList(_client.ServerConnectionId, schema, count), queryTimeout, cancellationToken);

        /// <summary>
        /// Returns a random sample of the documents in a schema as a result set of their fields.
        /// </summary>
        public KbQueryDocumentSampleReply Sample(string schema, int count, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryDocumentSample(_client.ServerConnectionId, schema, count), queryTimeout);

        /// <summary>
        /// Returns a random sample of the documents in a schema as a result set of their fields.
        /// </summary>
        public Task<KbQueryDocumentSampleReply> SampleAsync(string schema, int count, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryDocumentSample(_client.ServerConnectionId, schema, count), queryTimeout, cancellationToken);

        #endregion
    }
}
