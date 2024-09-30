using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads.RoundTrip;
using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Client.Management
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
        public KbQueryQueryExplainOperationReply ExplainOperation(string statement, KbInsensitiveDictionary<KbConstant>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryQueryExplainOperation(_client.ServerConnectionId, statement, userParameters), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        #endregion

        #region ExplainOperations.

        /// <summary>
        /// Explains the condition and join operations.
        /// </summary>
        public KbQueryQueryExplainOperationsReply ExplainOperations(List<string> statements, object? userParameters, TimeSpan? queryTimeout = null)
            => ExplainOperations(statements, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Explains the condition and join operations.
        /// </summary>
        public KbQueryQueryExplainOperationsReply ExplainOperations(List<string> statements, Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null)
            => ExplainOperations(statements, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Explains the condition and join operations.
        /// </summary>
        public KbQueryQueryExplainOperationsReply ExplainOperations(List<string> statements, KbInsensitiveDictionary<KbConstant>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryQueryExplainOperations(_client.ServerConnectionId, statements, userParameters), (TimeSpan)queryTimeout)
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
        public KbQueryQueryExplainPlanReply ExplainPlan(string statement, KbInsensitiveDictionary<KbConstant>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryQueryExplainPlan(_client.ServerConnectionId, statement, userParameters), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        #endregion

        #region ExplainPlans.

        /// <summary>
        /// Explains the condition and join plans, including applicable indexing.
        /// </summary>
        public KbQueryQueryExplainPlansReply ExplainPlans(List<string> statements, object? userParameters, TimeSpan? queryTimeout = null)
            => ExplainPlans(statements, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Explains the condition and join plans, including applicable indexing.
        /// </summary>
        public KbQueryQueryExplainPlansReply ExplainPlans(List<string> statements, Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null)
            => ExplainPlans(statements, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Explains the condition and join plans, including applicable indexing.
        /// </summary>
        public KbQueryQueryExplainPlansReply ExplainPlans(List<string> statements, KbInsensitiveDictionary<KbConstant>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryQueryExplainPlans(_client.ServerConnectionId, statements, userParameters), (TimeSpan)queryTimeout)
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
        public KbQueryQueryExecuteQueryReply Fetch(string statement, KbInsensitiveDictionary<KbConstant>? userParameters = null, TimeSpan? queryTimeout = null)
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
        public IEnumerable<T> Fetch<T>(string statement, KbInsensitiveDictionary<KbConstant>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
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

        #region FetchMultiple.

        /// <summary>
        /// Executes multiple statements and fetches their results given the supplied statement and optional parameters.
        /// </summary>
        public KbQueryQueryExecuteQueriesReply FetchMultiple(List<string> statements, object userParameters, TimeSpan? queryTimeout = null)
            => FetchMultiple(statements, userParameters.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Executes multiple statements and fetches their results given the supplied statement and optional parameters.
        /// </summary>
        public KbQueryQueryExecuteQueriesReply FetchMultiple(List<string> statements, Dictionary<string, object?> userParameters, TimeSpan? queryTimeout = null)
            => FetchMultiple(statements, userParameters.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Executes multiple statements and fetches their results given the supplied statement and optional parameters.
        /// </summary>
        public KbQueryQueryExecuteQueriesReply FetchMultiple(List<string> statements, KbInsensitiveDictionary<KbConstant>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryQueryExecuteQueries(_client.ServerConnectionId, statements, userParameters), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
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
        public KbQueryQueryExecuteNonQueryReply ExecuteNonQuery(string statement, KbInsensitiveDictionary<KbConstant>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryQueryExecuteNonQuery(_client.ServerConnectionId, statement, userParameters), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        #endregion

        //TODO: Add overloads for FirstFirst<>(), FirstSingle<T>(), FirstFirstOrDefault<>(), FirstSingleOrDefault<T>()
    }
}
