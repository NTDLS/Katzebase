using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads;

namespace NTDLS.Katzebase.Api.Management
{
    /// <summary>
    /// Index management. Set <see cref="KbIndex.IsUnique"/> to create a unique key.
    /// </summary>
    public class KbIndexesClient
    {
        private readonly KbClient _client;

        internal KbIndexesClient(KbClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Creates an index on the given schema and builds it from the schema's existing documents.
        /// Throws <see cref="Exceptions.KbDuplicateKeyViolationException"/> if a unique index is created over duplicate values.
        /// </summary>
        public void Create(string schema, KbIndex index, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryIndexCreate(_client.ServerConnectionId, schema, index), queryTimeout);

        /// <summary>
        /// Creates an index on the given schema and builds it from the schema's existing documents.
        /// </summary>
        public Task CreateAsync(string schema, KbIndex index, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryIndexCreate(_client.ServerConnectionId, schema, index), queryTimeout, cancellationToken);

        /// <summary>
        /// Checks for the existence of an index.
        /// </summary>
        public bool Exists(string schema, string indexName, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryIndexExists(_client.ServerConnectionId, schema, indexName), queryTimeout).Value;

        /// <summary>
        /// Checks for the existence of an index.
        /// </summary>
        public async Task<bool> ExistsAsync(string schema, string indexName, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => (await _client.SendAsync(new KbQueryIndexExists(_client.ServerConnectionId, schema, indexName), queryTimeout, cancellationToken).ConfigureAwait(false)).Value;

        /// <summary>
        /// Gets an index's definition, or null if it does not exist.
        /// </summary>
        public KbIndex? Get(string schema, string indexName, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryIndexGet(_client.ServerConnectionId, schema, indexName), queryTimeout).Index;

        /// <summary>
        /// Gets an index's definition, or null if it does not exist.
        /// </summary>
        public async Task<KbIndex?> GetAsync(string schema, string indexName, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => (await _client.SendAsync(new KbQueryIndexGet(_client.ServerConnectionId, schema, indexName), queryTimeout, cancellationToken).ConfigureAwait(false)).Index;

        /// <summary>
        /// Lists the indexes of a schema.
        /// </summary>
        public List<KbIndex> List(string schema, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryIndexList(_client.ServerConnectionId, schema), queryTimeout).Collection;

        /// <summary>
        /// Lists the indexes of a schema.
        /// </summary>
        public async Task<List<KbIndex>> ListAsync(string schema, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => (await _client.SendAsync(new KbQueryIndexList(_client.ServerConnectionId, schema), queryTimeout, cancellationToken).ConfigureAwait(false)).Collection;

        /// <summary>
        /// Rebuilds an index from the schema's documents.
        /// </summary>
        public void Rebuild(string schema, string indexName, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryIndexRebuild(_client.ServerConnectionId, schema, indexName), queryTimeout);

        /// <summary>
        /// Rebuilds an index from the schema's documents.
        /// </summary>
        public Task RebuildAsync(string schema, string indexName, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryIndexRebuild(_client.ServerConnectionId, schema, indexName), queryTimeout, cancellationToken);

        /// <summary>
        /// Drops an index.
        /// </summary>
        public void Drop(string schema, string indexName, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryIndexDrop(_client.ServerConnectionId, schema, indexName), queryTimeout);

        /// <summary>
        /// Drops an index.
        /// </summary>
        public Task DropAsync(string schema, string indexName, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryIndexDrop(_client.ServerConnectionId, schema, indexName), queryTimeout, cancellationToken);
    }
}
