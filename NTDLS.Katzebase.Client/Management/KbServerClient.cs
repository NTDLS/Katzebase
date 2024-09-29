using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads.RoundTrip;

namespace NTDLS.Katzebase.Client.Management
{
    public class KbServerClient
    {
        private readonly KbClient _client;

        public KbServerClient(KbClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Starts a session on the server
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KbAPIResponseException"></exception>
        public KbQueryServerStartSessionReply StartSession(string username, string passwordHash, string clientName, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            return _client.Connection.Query(
                new KbQueryServerStartSession(username, passwordHash, clientName), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        /// <summary>
        /// Closes the connected process on the server and rolls back any open transactions.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KbAPIResponseException"></exception>
        public void CloseSession(TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            _ = _client.Connection.Query(
                new KbQueryServerCloseSession(_client.ServerConnectionId), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }

        /// <summary>
        /// Terminates a process on the server and rolls back any open transactions.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="KbAPIResponseException"></exception>
        public void TerminateProcess(ulong processId, TimeSpan? queryTimeout = null)
        {
            if (_client.Connection?.IsConnected != true) throw new Exception("The client is not connected.");

            queryTimeout ??= _client.Connection.QueryTimeout;

            _ = _client.Connection.Query(
                new KbQueryServerTerminateProcess(_client.ServerConnectionId, processId), (TimeSpan)queryTimeout)
                .ContinueWith(t => _client.ValidateTaskResult(t)).Result;
        }
    }
}
