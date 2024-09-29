using NTDLS.Katzebase.Client.Payloads.RoundTrip;

namespace NTDLS.Katzebase.Client.Management
{
    public class KbTransactionClient
    {
        private readonly KbClient _client;

        public KbTransactionClient(KbClient client)
        {
            _client = client;
        }

        public void Begin(TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            _ = _client.Connection.Query(
                new KbQueryTransactionBegin(_client.ServerConnectionId), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        public void Commit(TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            _ = _client.Connection.Query(
                new KbQueryTransactionCommit(_client.ServerConnectionId), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        public void Rollback(TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            _ = _client.Connection.Query(
                new KbQueryTransactionRollback(_client.ServerConnectionId), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }
    }
}
