using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads;

namespace NTDLS.Katzebase.Api.Management
{
    /// <summary>
    /// Executes stored procedures. Procedures are identified by their fully qualified name: "Schema:Procedure"
    /// (a name without a schema refers to a procedure in the root schema). Parameters are given the same way as for
    /// <see cref="KbQueryClient"/>, see <see cref="KbParameters"/>.
    /// </summary>
    public class KbProcedureClient
    {
        private readonly KbClient _client;

        internal KbProcedureClient(KbClient client)
        {
            _client = client;
        }

        private KbQueryProcedureExecute MakeRequest(string procedureName, object? parameters)
            => new(_client.ServerConnectionId, new KbProcedure(procedureName, KbParameters.Convert(parameters)));

        /// <summary>
        /// Executes a procedure and returns all of its result sets.
        /// </summary>
        public KbQueryProcedureExecuteReply Execute(string procedureName, object? parameters = null, TimeSpan? queryTimeout = null)
            => _client.Send(MakeRequest(procedureName, parameters), queryTimeout);

        /// <summary>
        /// Executes a procedure and returns all of its result sets.
        /// </summary>
        public Task<KbQueryProcedureExecuteReply> ExecuteAsync(string procedureName, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(MakeRequest(procedureName, parameters), queryTimeout, cancellationToken);

        /// <summary>
        /// Executes a procedure that returns a single result set and maps its rows to objects.
        /// </summary>
        public List<T> Execute<T>(string procedureName, object? parameters = null, TimeSpan? queryTimeout = null) where T : new()
            => KbQueryClient.SingleResultSet(Execute(procedureName, parameters, queryTimeout)).MapTo<T>();

        /// <summary>
        /// Executes a procedure that returns a single result set and maps its rows to objects.
        /// </summary>
        public async Task<List<T>> ExecuteAsync<T>(string procedureName, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default) where T : new()
            => KbQueryClient.SingleResultSet(await ExecuteAsync(procedureName, parameters, queryTimeout, cancellationToken).ConfigureAwait(false)).MapTo<T>();

        /// <summary>
        /// Returns the first row returned by the procedure, throwing if there are none.
        /// </summary>
        public T ExecuteFirst<T>(string procedureName, object? parameters = null, TimeSpan? queryTimeout = null) where T : new()
            => KbQueryClient.First(Execute<T>(procedureName, parameters, queryTimeout));

        /// <summary>
        /// Returns the first row returned by the procedure, throwing if there are none.
        /// </summary>
        public async Task<T> ExecuteFirstAsync<T>(string procedureName, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default) where T : new()
            => KbQueryClient.First(await ExecuteAsync<T>(procedureName, parameters, queryTimeout, cancellationToken).ConfigureAwait(false));

        /// <summary>
        /// Returns the first row returned by the procedure, or default if there are none.
        /// </summary>
        public T? ExecuteFirstOrDefault<T>(string procedureName, object? parameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(procedureName, parameters, queryTimeout).FirstOrDefault();

        /// <summary>
        /// Returns the first row returned by the procedure, or default if there are none.
        /// </summary>
        public async Task<T?> ExecuteFirstOrDefaultAsync<T>(string procedureName, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default) where T : new()
            => (await ExecuteAsync<T>(procedureName, parameters, queryTimeout, cancellationToken).ConfigureAwait(false)).FirstOrDefault();

        /// <summary>
        /// Returns the only row returned by the procedure, throwing if there are none or more than one.
        /// </summary>
        public T ExecuteSingle<T>(string procedureName, object? parameters = null, TimeSpan? queryTimeout = null) where T : new()
            => KbQueryClient.Single(Execute<T>(procedureName, parameters, queryTimeout), allowNone: false)!;

        /// <summary>
        /// Returns the only row returned by the procedure, throwing if there are none or more than one.
        /// </summary>
        public async Task<T> ExecuteSingleAsync<T>(string procedureName, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default) where T : new()
            => KbQueryClient.Single(await ExecuteAsync<T>(procedureName, parameters, queryTimeout, cancellationToken).ConfigureAwait(false), allowNone: false)!;

        /// <summary>
        /// Returns the only row returned by the procedure, or default if there are none. Throws if there is more than one.
        /// </summary>
        public T? ExecuteSingleOrDefault<T>(string procedureName, object? parameters = null, TimeSpan? queryTimeout = null) where T : new()
            => KbQueryClient.Single(Execute<T>(procedureName, parameters, queryTimeout), allowNone: true);

        /// <summary>
        /// Returns the only row returned by the procedure, or default if there are none. Throws if there is more than one.
        /// </summary>
        public async Task<T?> ExecuteSingleOrDefaultAsync<T>(string procedureName, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default) where T : new()
            => KbQueryClient.Single(await ExecuteAsync<T>(procedureName, parameters, queryTimeout, cancellationToken).ConfigureAwait(false), allowNone: true);

        /// <summary>
        /// Returns the first column of the first row returned by the procedure, or default if there are no rows.
        /// </summary>
        public T? ExecuteScalar<T>(string procedureName, object? parameters = null, TimeSpan? queryTimeout = null)
            => KbQueryClient.Scalar<T>(Execute(procedureName, parameters, queryTimeout));

        /// <summary>
        /// Returns the first column of the first row returned by the procedure, or default if there are no rows.
        /// </summary>
        public async Task<T?> ExecuteScalarAsync<T>(string procedureName, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => KbQueryClient.Scalar<T>(await ExecuteAsync(procedureName, parameters, queryTimeout, cancellationToken).ConfigureAwait(false));
    }
}
