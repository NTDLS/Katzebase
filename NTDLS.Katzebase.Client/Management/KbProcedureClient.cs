using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Client.Payloads.RoundTrip;
using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Client.Management
{
    public class KbProcedureClient
    {
        private readonly KbClient _client;

        public KbIndexesClient Indexes { get; set; }

        public KbProcedureClient(KbClient client)
        {
            _client = client;
            Indexes = new KbIndexesClient(client);
        }

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        public KbQueryProcedureExecuteReply Execute(string fullyQualifiedProcedureName, object? userParameters = null, TimeSpan? queryTimeout = null)
            => Execute(fullyQualifiedProcedureName, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        public KbQueryProcedureExecuteReply Execute(string fullyQualifiedProcedureName, Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null)
            => Execute(fullyQualifiedProcedureName, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        public KbQueryProcedureExecuteReply Execute(string fullyQualifiedProcedureName, KbInsensitiveDictionary<KbConstant>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            var procedure = new KbProcedure(fullyQualifiedProcedureName, userParameters);

            return _client.Connection.Query(
                new KbQueryProcedureExecute(_client.ServerConnectionId, procedure), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        public KbQueryProcedureExecuteReply Execute(string schemaName, string procedureName, object? userParameters = null, TimeSpan? queryTimeout = null)
            => Execute(schemaName, procedureName, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        public KbQueryProcedureExecuteReply Execute(string schemaName, string procedureName, Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null)
            => Execute(schemaName, procedureName, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        public KbQueryProcedureExecuteReply Execute(string schemaName, string procedureName, KbInsensitiveDictionary<KbConstant>? userParameters = null, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            var procedure = new KbProcedure(schemaName, procedureName, userParameters);

            return _client.Connection.Query(
                new KbQueryProcedureExecute(_client.ServerConnectionId, procedure), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        public IEnumerable<T> Execute<T>(string schemaName, string procedureName, object? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(schemaName, procedureName, userParameters.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        public IEnumerable<T> Execute<T>(string schemaName, string procedureName, Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(schemaName, procedureName, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        public IEnumerable<T> Execute<T>(string schemaName, string procedureName, KbInsensitiveDictionary<KbConstant>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            var procedure = new KbProcedure(schemaName, procedureName, userParameters?.ToUserParametersInsensitiveDictionary());

            var resultCollection = _client.Connection.Query(
                new KbQueryProcedureExecute(_client.ServerConnectionId, procedure), (TimeSpan)queryTimeout)
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

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        IEnumerable<T> Execute<T>(string fullyQualifiedProcedureName, object? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(fullyQualifiedProcedureName, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        IEnumerable<T> Execute<T>(string fullyQualifiedProcedureName, Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(fullyQualifiedProcedureName, userParameters?.ToUserParametersInsensitiveDictionary(), queryTimeout);

        /// <summary>
        /// Executes a procedure with or without parameters. This method of calling a procedure performs various types of validation.
        /// </summary>
        public IEnumerable<T> Execute<T>(string fullyQualifiedProcedureName,
            KbInsensitiveDictionary<KbConstant>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            var procedure = new KbProcedure(fullyQualifiedProcedureName, userParameters);

            var resultCollection = _client.Connection.Query(
                new KbQueryProcedureExecute(_client.ServerConnectionId, procedure), (TimeSpan)queryTimeout)
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

        //--Execute single, singleOrDefault, first, firstOrDefault by fully qualified procedure name using anonymous parameter type.

        public T ExecuteSingle<T>(string fullyQualifiedProcedureName,
            object? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(fullyQualifiedProcedureName, userParameters, queryTimeout).Single();

        public T? ExecuteSingleOrDefault<T>(string fullyQualifiedProcedureName,
            object? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(fullyQualifiedProcedureName, userParameters, queryTimeout).SingleOrDefault();

        public T ExecuteFirst<T>(string fullyQualifiedProcedureName,
            object? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(fullyQualifiedProcedureName, userParameters, queryTimeout).First();

        public T? ExecuteFirstOrDefault<T>(string fullyQualifiedProcedureName,
            object? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(fullyQualifiedProcedureName, userParameters, queryTimeout).FirstOrDefault();

        //--Execute single, singleOrDefault, first, firstOrDefault by specific schema and procedure name using anonymous parameter type.

        public T ExecuteSingle<T>(string schemaName, string procedureName,
            object? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(schemaName, procedureName, userParameters, queryTimeout).Single();

        public T? ExecuteSingleOrDefault<T>(string schemaName, string procedureName,
            object? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(schemaName, procedureName, userParameters, queryTimeout).SingleOrDefault();

        public T ExecuteFirst<T>(string schemaName, string procedureName,
            object? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(schemaName, procedureName, userParameters, queryTimeout).First();

        public T? ExecuteFirstOrDefault<T>(string schemaName, string procedureName,
            object? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(schemaName, procedureName, userParameters, queryTimeout).FirstOrDefault();

        //--Execute single, singleOrDefault, first, firstOrDefault by fully qualified procedure name using parameter collection.
        public T ExecuteSingle<T>(string fullyQualifiedProcedureName,
            Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(fullyQualifiedProcedureName, userParameters, queryTimeout).Single();

        public T? ExecuteSingleOrDefault<T>(string fullyQualifiedProcedureName,
            Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(fullyQualifiedProcedureName, userParameters, queryTimeout).SingleOrDefault();

        public T ExecuteFirst<T>(string fullyQualifiedProcedureName,
            Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(fullyQualifiedProcedureName, userParameters, queryTimeout).First();

        public T? ExecuteFirstOrDefault<T>(string fullyQualifiedProcedureName,
            Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(fullyQualifiedProcedureName, userParameters, queryTimeout).FirstOrDefault();

        //--Execute single, singleOrDefault, first, firstOrDefault by specific schema and procedure name using parameter collection.
        public T ExecuteSingle<T>(string schemaName, string procedureName,
            Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(schemaName, procedureName, userParameters, queryTimeout).Single();

        public T? ExecuteSingleOrDefault<T>(string schemaName, string procedureName,
            Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(schemaName, procedureName, userParameters, queryTimeout).SingleOrDefault();

        public T ExecuteFirst<T>(string schemaName, string procedureName,
            Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(schemaName, procedureName, userParameters, queryTimeout).First();

        public T? ExecuteFirstOrDefault<T>(string schemaName, string procedureName,
            Dictionary<string, object?>? userParameters = null, TimeSpan? queryTimeout = null) where T : new()
            => Execute<T>(schemaName, procedureName, userParameters, queryTimeout).FirstOrDefault();
    }
}
