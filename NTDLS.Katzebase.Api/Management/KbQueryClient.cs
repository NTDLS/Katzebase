using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Api.Types;

namespace NTDLS.Katzebase.Api.Management
{
    public class KbQueryClient
    {
        private readonly KbClient _client;

        public KbQueryClient(KbClient client)
        {
            _client = client;
        }

        #region ExplainOperation.

        /// <summary>
        /// Explains the condition and join operations.
        /// </summary>
        public KbQueryQueryExplainOperationReply ExplainOperation(string statement, object? userParameters, TimeSpan? queryTimeout = null)
            => ExplainOperation(statement, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Explains the condition and join operations.
        /// </summary>
        public KbQueryQueryExplainOperationReply ExplainOperation(string statement, Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null)
            => ExplainOperation(statement, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Explains the condition and join operations.
        /// </summary>
        public KbQueryQueryExplainOperationReply ExplainOperation(string statement, KbInsensitiveDictionary<KbVariable>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryQueryExplainOperation(_client.ServerConnectionId, statement, userParameters), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        #endregion

        #region ExplainPlan.

        /// <summary>
        /// Explains the condition and join plans, including applicable indexing.
        /// </summary>
        public KbQueryQueryExplainPlanReply ExplainPlan(string statement, object? userParameters, TimeSpan? queryTimeout = null)
            => ExplainPlan(statement, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Explains the condition and join plans, including applicable indexing.
        /// </summary>
        public KbQueryQueryExplainPlanReply ExplainPlan(string statement, Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null)
            => ExplainPlan(statement, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Explains the condition and join plans, including applicable indexing.
        /// </summary>
        public KbQueryQueryExplainPlanReply ExplainPlan(string statement, KbInsensitiveDictionary<KbVariable>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryQueryExplainPlan(_client.ServerConnectionId, statement, userParameters), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        #endregion

        #region Fetch.

        /// <summary>
        /// Fetches documents using the given query and optional parameters.
        /// </summary>
        public KbQueryQueryExecuteQueryReply Fetch(string statement, object userParameters, TimeSpan? queryTimeout = null)
            => Fetch(statement, userParameters.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Fetches documents using the given query and optional parameters.
        /// </summary>
        public KbQueryQueryExecuteQueryReply Fetch(string statement, Dictionary<string, object?> userParameters, TimeSpan? queryTimeout = null)
            => Fetch(statement, userParameters.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Fetches documents using the given query and optional parameters.
        /// </summary>
        public KbQueryQueryExecuteQueryReply Fetch(string statement, KbInsensitiveDictionary<KbVariable>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryQueryExecuteQuery(_client.ServerConnectionId, statement, userParameters), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        #endregion

        #region Fetch<T>.

        /// <summary>
        /// Fetches documents using the given query and optional parameters.
        /// </summary>
        public IEnumerable<T> Fetch<T>(string statement, object userParameters, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Fetches documents using the given query and optional parameters.
        /// </summary>
        public IEnumerable<T> Fetch<T>(string statement, Dictionary<string, object?> userParameters, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Fetches documents using the given query and optional parameters.
        /// </summary>
        public IEnumerable<T> Fetch<T>(string statement, KbInsensitiveDictionary<KbVariable>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            var resultCollection = _client.Connection.Query(
                new KbQueryQueryExecuteQuery(_client.ServerConnectionId, statement, userParameters), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;

            if (resultCollection.Collection.Count > 1)
            {
                throw new KbMultipleRecordSetsException();
            }
            else if (resultCollection.Collection.Count == 0)
            {
                return new List<T>();
            }

            return resultCollection.Collection[0].MapTo<T>();
        }

        #endregion

        #region FetchFirst<T>.

        /// <summary>
        /// Fetches the first row, or throws an exception, using the given query and optional parameters.
        /// </summary>
        public T FetchFirst<T>(string statement, object userParameters, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters, queryTimeout).First();

        /// <summary>
        /// Fetches the first row, or throws an exception, using the given query and optional parameters.
        /// </summary>
        public T FetchFirst<T>(string statement, Dictionary<string, object?> userParameters, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters, queryTimeout).First();

        /// <summary>
        /// Fetches the first row, or throws an exception, using the given query and optional parameters.
        /// </summary>
        public T FetchFirst<T>(string statement, KbInsensitiveDictionary<KbVariable>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters, queryTimeout).First();

        #endregion

        #region FetchFirstOrDefault<T>.

        /// <summary>
        /// Fetches the first row, or null using the given query and optional parameters.
        /// </summary>
        public T? FetchFirstOrDefault<T>(string statement, object userParameters, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters, queryTimeout).FirstOrDefault();

        /// <summary>
        /// Fetches the first row, or null using the given query and optional parameters.
        /// </summary>
        public T? FetchFirstOrDefault<T>(string statement, Dictionary<string, object?> userParameters, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters, queryTimeout).FirstOrDefault();

        /// <summary>
        /// Fetches the first row, or null using the given query and optional parameters.
        /// </summary>
        public T? FetchFirstOrDefault<T>(string statement, KbInsensitiveDictionary<KbVariable>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters, queryTimeout).FirstOrDefault();

        #endregion

        #region FetchSingle<T>.

        /// <summary>
        /// Fetches a single row, or throws an exception if more than one row is present, using the given query and optional parameters.
        /// </summary>
        public T FetchSingle<T>(string statement, object userParameters, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters, queryTimeout).Single();

        /// <summary>
        /// Fetches a single row, or throws an exception if more than one row is present, using the given query and optional parameters.
        /// </summary>
        public T FetchSingle<T>(string statement, Dictionary<string, object?> userParameters, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters, queryTimeout).Single();

        /// <summary>
        /// Fetches a single row, or throws an exception if more than one row is present, using the given query and optional parameters.
        /// </summary>
        public T FetchSingle<T>(string statement, KbInsensitiveDictionary<KbVariable>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters, queryTimeout).Single();

        #endregion

        #region FetchSingleOrDefault<T>.

        /// <summary>
        /// Fetches a single row, or null, throws an exception if more than one row is present, using the given query and optional parameters.
        /// </summary>
        public T? FetchSingleOrDefault<T>(string statement, object userParameters, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters, queryTimeout).SingleOrDefault();

        /// <summary>
        /// Fetches a single row, or null, throws an exception if more than one row is present, using the given query and optional parameters.
        /// </summary>
        public T? FetchSingleOrDefault<T>(string statement, Dictionary<string, object?> userParameters, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters, queryTimeout).SingleOrDefault();

        /// <summary>
        /// Fetches a single row, or null, throws an exception if more than one row is present, using the given query and optional parameters.
        /// </summary>
        public T? FetchSingleOrDefault<T>(string statement, KbInsensitiveDictionary<KbVariable>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Fetch<T>(statement, userParameters, queryTimeout).SingleOrDefault();

        #endregion

        #region FetchScalar<T>.

        /// <summary>
        /// Fetches a single row, or throws an exception if more than one row is present, using the given query and optional parameters.
        /// </summary>
        public T? FetchScalar<T>(string statement, object userParameters, TimeSpan? queryTimeout = null)
        {
            var result = Fetch(statement, userParameters.ToUserParametersInsensitiveDictionary(), queryTimeout);
            var firstValue = result.Collection.Single().Rows.FirstOrDefault()?.Values?.FirstOrDefault();
            return Converters.ConvertToNullable<T>(firstValue);
        }

        /// <summary>
        /// Fetches a single row, or throws an exception if more than one row is present, using the given query and optional parameters.
        /// </summary>
        public T? FetchScalar<T>(string statement, Dictionary<string, object?> userParameters, TimeSpan? queryTimeout = null)
        {
            var result = Fetch(statement, userParameters.ToUserParametersInsensitiveDictionary(), queryTimeout);
            var firstValue = result.Collection.Single().Rows.FirstOrDefault()?.Values?.FirstOrDefault();
            return Converters.ConvertToNullable<T>(firstValue);
        }

        /// <summary>
        /// Fetches a single row, or throws an exception if more than one row is present, using the given query and optional parameters.
        /// </summary>
        public T? FetchScalar<T>(string statement, KbInsensitiveDictionary<KbVariable>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            var result = Fetch(statement, userParameters, queryTimeout);
            var firstValue = result.Collection.Single().Rows.FirstOrDefault()?.Values?.FirstOrDefault();
            return Converters.ConvertToNullable<T>(firstValue);
        }

        #endregion

        #region ExecuteNonQuery.

        /// <summary>
        /// Executes a statements using the supplied statement and optional parameters.
        /// </summary>
        public KbQueryQueryExecuteNonQueryReply ExecuteNonQuery(string statement, object userParameters, TimeSpan? queryTimeout = null)
            => ExecuteNonQuery(statement, userParameters.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Executes a statements using the supplied statement and optional parameters.
        /// </summary>
        public KbQueryQueryExecuteNonQueryReply ExecuteNonQuery(string statement, Dictionary<string, object?> userParameters, TimeSpan? queryTimeout = null)
            => ExecuteNonQuery(statement, userParameters.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Executes a statements using the supplied statement and optional parameters.
        /// </summary>
        public KbQueryQueryExecuteNonQueryReply ExecuteNonQuery(string statement, KbInsensitiveDictionary<KbVariable>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryQueryExecuteNonQuery(_client.ServerConnectionId, statement, userParameters), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        #endregion
    }
}
