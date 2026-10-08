using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Api.Payloads.Response;

namespace NTDLS.Katzebase.Api.Management
{
    /// <summary>
    /// Executes statements.
    ///
    /// Parameters are referenced in statements as @Name and can be given as an anonymous object (new { Name = "x" }), any other
    /// object (its public properties), or a dictionary of names to values. See <see cref="KbParameters"/>.
    /// Always pass values as parameters rather than formatting them into the statement text.
    /// </summary>
    public class KbQueryClient
    {
        private readonly KbClient _client;

        internal KbQueryClient(KbClient client)
        {
            _client = client;
        }

        #region Fetch.

        /// <summary>
        /// Executes a statement (or batch of statements) and returns all of the result sets.
        /// </summary>
        public KbQueryQueryExecuteQueryReply Fetch(string statement, object? parameters = null, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryQueryExecuteQuery(_client.ServerConnectionId, statement, KbParameters.Convert(parameters)), queryTimeout);

        /// <summary>
        /// Executes a statement (or batch of statements) and returns all of the result sets.
        /// </summary>
        public KbQueryQueryExecuteQueryReply Fetch(string statement, TimeSpan queryTimeout)
            => Fetch(statement, null, queryTimeout);

        /// <summary>
        /// Executes a statement (or batch of statements) and returns all of the result sets.
        /// </summary>
        public Task<KbQueryQueryExecuteQueryReply> FetchAsync(string statement, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryQueryExecuteQuery(_client.ServerConnectionId, statement, KbParameters.Convert(parameters)), queryTimeout, cancellationToken);

        /// <summary>
        /// Executes a statement that returns a single result set and maps its rows to objects (see <see cref="KbExtensions.MapTo{T}"/>).
        /// </summary>
        public List<T> Fetch<T>(string statement, object? parameters = null, TimeSpan? queryTimeout = null) where T : new()
            => SingleResultSet(Fetch(statement, parameters, queryTimeout)).MapTo<T>();

        /// <summary>
        /// Executes a statement that returns a single result set and maps its rows to objects (see <see cref="KbExtensions.MapTo{T}"/>).
        /// </summary>
        public async Task<List<T>> FetchAsync<T>(string statement, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default) where T : new()
            => SingleResultSet(await FetchAsync(statement, parameters, queryTimeout, cancellationToken).ConfigureAwait(false)).MapTo<T>();

        /// <summary>
        /// Returns the first row of the result, throwing <see cref="KbObjectNotFoundException"/> if there are no rows.
        /// </summary>
        public T FetchFirst<T>(string statement, object? parameters = null, TimeSpan? queryTimeout = null) where T : new()
            => First(Fetch<T>(statement, parameters, queryTimeout));

        /// <summary>
        /// Returns the first row of the result, throwing <see cref="KbObjectNotFoundException"/> if there are no rows.
        /// </summary>
        public async Task<T> FetchFirstAsync<T>(string statement, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default) where T : new()
            => First(await FetchAsync<T>(statement, parameters, queryTimeout, cancellationToken).ConfigureAwait(false));

        /// <summary>
        /// Returns the first row of the result, or default if there are no rows.
        /// </summary>
        public T? FetchFirstOrDefault<T>(string statement, object? parameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, parameters, queryTimeout).FirstOrDefault();

        /// <summary>
        /// Returns the first row of the result, or default if there are no rows.
        /// </summary>
        public async Task<T?> FetchFirstOrDefaultAsync<T>(string statement, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default) where T : new()
            => (await FetchAsync<T>(statement, parameters, queryTimeout, cancellationToken).ConfigureAwait(false)).FirstOrDefault();

        /// <summary>
        /// Returns the only row of the result, throwing <see cref="KbObjectNotFoundException"/> if there are no rows
        /// and <see cref="KbProcessingException"/> if there is more than one.
        /// </summary>
        public T FetchSingle<T>(string statement, object? parameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Single(Fetch<T>(statement, parameters, queryTimeout), allowNone: false)!;

        /// <summary>
        /// Returns the only row of the result. See <see cref="FetchSingle{T}"/>.
        /// </summary>
        public async Task<T> FetchSingleAsync<T>(string statement, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default) where T : new()
            => Single(await FetchAsync<T>(statement, parameters, queryTimeout, cancellationToken).ConfigureAwait(false), allowNone: false)!;

        /// <summary>
        /// Returns the only row of the result, or default if there are no rows. Throws <see cref="KbProcessingException"/> if there is more than one.
        /// </summary>
        public T? FetchSingleOrDefault<T>(string statement, object? parameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Single(Fetch<T>(statement, parameters, queryTimeout), allowNone: true);

        /// <summary>
        /// Returns the only row of the result, or default if there are no rows. See <see cref="FetchSingleOrDefault{T}"/>.
        /// </summary>
        public async Task<T?> FetchSingleOrDefaultAsync<T>(string statement, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default) where T : new()
            => Single(await FetchAsync<T>(statement, parameters, queryTimeout, cancellationToken).ConfigureAwait(false), allowNone: true);

        /// <summary>
        /// Returns the first column of the first row of the result, converted to T, or default if there are no rows.
        /// </summary>
        public T? FetchScalar<T>(string statement, object? parameters = null, TimeSpan? queryTimeout = null)
            => Scalar<T>(Fetch(statement, parameters, queryTimeout));

        /// <summary>
        /// Returns the first column of the first row of the result, converted to T, or default if there are no rows.
        /// </summary>
        public async Task<T?> FetchScalarAsync<T>(string statement, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => Scalar<T>(await FetchAsync(statement, parameters, queryTimeout, cancellationToken).ConfigureAwait(false));

        #endregion

        #region ExecuteNonQuery.

        /// <summary>
        /// Executes a statement (or batch of statements) that does not return rows, e.g. INSERT, UPDATE, DELETE or DDL.
        /// The reply contains the number of affected rows for each statement.
        /// </summary>
        public KbQueryQueryExecuteNonQueryReply ExecuteNonQuery(string statement, object? parameters = null, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryQueryExecuteNonQuery(_client.ServerConnectionId, statement, KbParameters.Convert(parameters)), queryTimeout);

        /// <summary>
        /// Executes a statement (or batch of statements) that does not return rows.
        /// </summary>
        public KbQueryQueryExecuteNonQueryReply ExecuteNonQuery(string statement, TimeSpan queryTimeout)
            => ExecuteNonQuery(statement, null, queryTimeout);

        /// <summary>
        /// Executes a statement (or batch of statements) that does not return rows.
        /// </summary>
        public Task<KbQueryQueryExecuteNonQueryReply> ExecuteNonQueryAsync(string statement, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryQueryExecuteNonQuery(_client.ServerConnectionId, statement, KbParameters.Convert(parameters)), queryTimeout, cancellationToken);

        #endregion

        #region Explain.

        /// <summary>
        /// Explains how the statement's conditions and joins would be evaluated, including which indexes would be used.
        /// </summary>
        public KbQueryQueryExplainPlanReply ExplainPlan(string statement, object? parameters = null, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryQueryExplainPlan(_client.ServerConnectionId, statement, KbParameters.Convert(parameters)), queryTimeout);

        /// <summary>
        /// Explains how the statement's conditions and joins would be evaluated, including which indexes would be used.
        /// </summary>
        public KbQueryQueryExplainPlanReply ExplainPlan(string statement, TimeSpan queryTimeout)
            => ExplainPlan(statement, null, queryTimeout);

        /// <summary>
        /// Explains how the statement's conditions and joins would be evaluated, including which indexes would be used.
        /// </summary>
        public Task<KbQueryQueryExplainPlanReply> ExplainPlanAsync(string statement, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryQueryExplainPlan(_client.ServerConnectionId, statement, KbParameters.Convert(parameters)), queryTimeout, cancellationToken);

        /// <summary>
        /// Explains the condition and join operations of the statement.
        /// </summary>
        public KbQueryQueryExplainOperationReply ExplainOperation(string statement, object? parameters = null, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryQueryExplainOperation(_client.ServerConnectionId, statement, KbParameters.Convert(parameters)), queryTimeout);

        /// <summary>
        /// Explains the condition and join operations of the statement.
        /// </summary>
        public KbQueryQueryExplainOperationReply ExplainOperation(string statement, TimeSpan queryTimeout)
            => ExplainOperation(statement, null, queryTimeout);

        /// <summary>
        /// Explains the condition and join operations of the statement.
        /// </summary>
        public Task<KbQueryQueryExplainOperationReply> ExplainOperationAsync(string statement, object? parameters = null,
            TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryQueryExplainOperation(_client.ServerConnectionId, statement, KbParameters.Convert(parameters)), queryTimeout, cancellationToken);

        #endregion

        #region Result helpers.

        /// <summary>
        /// Returns the only result set of a reply. Statements that return no result set (e.g. DDL) yield an empty one.
        /// </summary>
        internal static KbQueryResult SingleResultSet(KbQueryResultCollection results)
        {
            return results.Collection.Count switch
            {
                0 => new KbQueryResult(),
                1 => results.Collection[0],
                _ => throw new KbMultipleRecordSetsException($"Expected a single result set, but the statement returned {results.Collection.Count}.")
            };
        }

        internal static T First<T>(List<T> rows)
            => rows.Count > 0 ? rows[0] : throw new KbObjectNotFoundException("The statement returned no rows.");

        internal static T? Single<T>(List<T> rows, bool allowNone)
        {
            return rows.Count switch
            {
                0 when allowNone => default,
                0 => throw new KbObjectNotFoundException("The statement returned no rows."),
                1 => rows[0],
                _ => throw new KbProcessingException($"Expected a single row, but the statement returned {rows.Count}.")
            };
        }

        internal static T? Scalar<T>(KbQueryResultCollection results)
        {
            var row = SingleResultSet(results).Rows.FirstOrDefault();
            return row == null || row.Values.Count == 0 ? default : KbValueConverter.Convert<T>(row.Values[0]);
        }

        #endregion
    }
}
