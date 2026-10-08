using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads;

namespace NTDLS.Katzebase.Api.Management
{
    /// <summary>
    /// Schema (namespace) management. Schema names are colon-delimited paths, e.g. "Sales:Orders".
    /// </summary>
    public class KbSchemaClient
    {
        private readonly KbClient _client;

        public KbIndexesClient Indexes { get; }

        internal KbSchemaClient(KbClient client)
        {
            _client = client;
            Indexes = new KbIndexesClient(client);
        }

        #region Create.

        /// <summary>
        /// Creates a single schema. The parent schema must already exist; does nothing if the schema already exists.
        /// </summary>
        public void Create(string schema, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQuerySchemaCreate(_client.ServerConnectionId, schema), queryTimeout);

        /// <summary>
        /// Creates a single schema. The parent schema must already exist; does nothing if the schema already exists.
        /// </summary>
        public Task CreateAsync(string schema, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQuerySchemaCreate(_client.ServerConnectionId, schema), queryTimeout, cancellationToken);

        /// <summary>
        /// Creates a schema and any of its parents that do not exist.
        /// </summary>
        public void CreateRecursive(string schema, TimeSpan? queryTimeout = null)
        {
            foreach (var path in AncestorsAndSelf(schema))
            {
                Create(path, queryTimeout);
            }
        }

        /// <summary>
        /// Creates a schema and any of its parents that do not exist.
        /// </summary>
        public async Task CreateRecursiveAsync(string schema, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
        {
            foreach (var path in AncestorsAndSelf(schema))
            {
                await CreateAsync(path, queryTimeout, cancellationToken).ConfigureAwait(false);
            }
        }

        private static IEnumerable<string> AncestorsAndSelf(string schema)
        {
            var segments = schema.Trim().Trim(':').Split(':');
            for (int i = 1; i <= segments.Length; i++)
            {
                yield return string.Join(':', segments.Take(i));
            }
        }

        #endregion

        #region Exists.

        /// <summary>
        /// Checks for the existence of a schema.
        /// </summary>
        public bool Exists(string schema, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQuerySchemaExists(_client.ServerConnectionId, schema), queryTimeout).Value;

        /// <summary>
        /// Checks for the existence of a schema.
        /// </summary>
        public async Task<bool> ExistsAsync(string schema, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => (await _client.SendAsync(new KbQuerySchemaExists(_client.ServerConnectionId, schema), queryTimeout, cancellationToken).ConfigureAwait(false)).Value;

        #endregion

        #region Drop.

        /// <summary>
        /// Drops a schema, including all of its documents, indexes and child schemas. Does nothing if it does not exist.
        /// </summary>
        public void Drop(string schema, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQuerySchemaDrop(_client.ServerConnectionId, schema), queryTimeout);

        /// <summary>
        /// Drops a schema, including all of its documents, indexes and child schemas. Does nothing if it does not exist.
        /// </summary>
        public Task DropAsync(string schema, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQuerySchemaDrop(_client.ServerConnectionId, schema), queryTimeout, cancellationToken);

        /// <summary>
        /// Drops a schema if it exists, returning true if it did.
        /// </summary>
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
        /// Drops a schema if it exists, returning true if it did.
        /// </summary>
        public async Task<bool> DropIfExistsAsync(string schema, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
        {
            if (await ExistsAsync(schema, queryTimeout, cancellationToken).ConfigureAwait(false))
            {
                await DropAsync(schema, queryTimeout, cancellationToken).ConfigureAwait(false);
                return true;
            }
            return false;
        }

        #endregion

        #region List.

        /// <summary>
        /// Lists the child schemas of the given schema.
        /// </summary>
        public KbQuerySchemaListReply List(string schema, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQuerySchemaList(_client.ServerConnectionId, schema), queryTimeout);

        /// <summary>
        /// Lists the child schemas of the given schema.
        /// </summary>
        public Task<KbQuerySchemaListReply> ListAsync(string schema, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQuerySchemaList(_client.ServerConnectionId, schema), queryTimeout, cancellationToken);

        /// <summary>
        /// Lists the top-level schemas.
        /// </summary>
        public KbQuerySchemaListReply List(TimeSpan? queryTimeout = null)
            => List(":", queryTimeout);

        /// <summary>
        /// Lists the top-level schemas.
        /// </summary>
        public Task<KbQuerySchemaListReply> ListAsync(TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => ListAsync(":", queryTimeout, cancellationToken);

        #endregion

        #region Field sample.

        /// <summary>
        /// Returns the field names found in a sample of the schema's documents.
        /// </summary>
        public KbQuerySchemaFieldSampleReply FieldSample(string schema, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQuerySchemaFieldSample(_client.ServerConnectionId, schema), queryTimeout);

        /// <summary>
        /// Returns the field names found in a sample of the schema's documents.
        /// </summary>
        public Task<KbQuerySchemaFieldSampleReply> FieldSampleAsync(string schema, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQuerySchemaFieldSample(_client.ServerConnectionId, schema), queryTimeout, cancellationToken);

        #endregion

        #region Attach / detach.

        /// <summary>
        /// Copies a namespace (a schema folder, with its documents, indexes and child schemas) from a folder on the server into
        /// the database as the given, not yet existing, schema. The source folder is not modified. Requires an administrator.
        /// </summary>
        /// <param name="folderPath">An absolute path on the server.</param>
        public KbQueryQueryExecuteNonQueryReply Attach(string schema, string folderPath, TimeSpan? queryTimeout = null)
            => _client.Query.ExecuteNonQuery($"ATTACH SCHEMA {ValidateSchemaName(schema)} FROM @FolderPath", new { FolderPath = folderPath }, queryTimeout);

        /// <summary>
        /// Copies a namespace from a folder on the server into the database. See <see cref="Attach"/>.
        /// </summary>
        public Task<KbQueryQueryExecuteNonQueryReply> AttachAsync(string schema, string folderPath, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.Query.ExecuteNonQueryAsync($"ATTACH SCHEMA {ValidateSchemaName(schema)} FROM @FolderPath", new { FolderPath = folderPath }, queryTimeout, cancellationToken);

        /// <summary>
        /// Removes a schema (with its documents, indexes and child schemas) from the database and moves its files to the
        /// given, not yet existing, folder on the server, from where it can be attached again. Requires an administrator.
        /// </summary>
        /// <param name="folderPath">An absolute path on the server.</param>
        public KbQueryQueryExecuteNonQueryReply Detach(string schema, string folderPath, TimeSpan? queryTimeout = null)
            => _client.Query.ExecuteNonQuery($"DETACH SCHEMA {ValidateSchemaName(schema)} TO @FolderPath", new { FolderPath = folderPath }, queryTimeout);

        /// <summary>
        /// Removes a schema from the database and moves its files to a folder on the server. See <see cref="Detach"/>.
        /// </summary>
        public Task<KbQueryQueryExecuteNonQueryReply> DetachAsync(string schema, string folderPath, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.Query.ExecuteNonQueryAsync($"DETACH SCHEMA {ValidateSchemaName(schema)} TO @FolderPath", new { FolderPath = folderPath }, queryTimeout, cancellationToken);

        #endregion

        /// <summary>
        /// Schema names are placed into statements, so they are restricted to the characters that make up a schema identifier.
        /// </summary>
        internal static string ValidateSchemaName(string schema)
        {
            var name = schema?.Trim() ?? string.Empty;
            if (name.Length == 0 || name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == ':' || c == '#') == false)
            {
                throw new KbInvalidArgumentException($"Invalid schema name: [{schema}]. Schema names may only contain letters, digits, '_', '#' and ':'.");
            }
            return name;
        }
    }
}
