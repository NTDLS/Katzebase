using NTDLS.Katzebase.Api.Payloads;

namespace NTDLS.Katzebase.Api.Management
{
    /// <summary>
    /// Server and session management.
    /// </summary>
    public class KbServerClient
    {
        private readonly KbClient _client;

        internal KbServerClient(KbClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Starts a session on the server. Called by <see cref="KbClient.Connect"/>.
        /// </summary>
        internal KbQueryServerStartSessionReply StartSession(string username, string passwordHash, string clientName, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryServerStartSession(username, passwordHash, clientName), queryTimeout);

        /// <summary>
        /// Closes the session on the server, rolling back any open transaction. Called by <see cref="KbClient.Disconnect"/>.
        /// </summary>
        internal void CloseSession(TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryServerCloseSession(_client.ServerConnectionId), queryTimeout);

        /// <summary>
        /// Terminates a process (session) on the server and rolls back its open transaction.
        /// </summary>
        public void TerminateProcess(ulong processId, TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryServerTerminateProcess(_client.ServerConnectionId, processId), queryTimeout);

        /// <summary>
        /// Terminates a process (session) on the server and rolls back its open transaction.
        /// </summary>
        public Task TerminateProcessAsync(ulong processId, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryServerTerminateProcess(_client.ServerConnectionId, processId), queryTimeout, cancellationToken);
    }
}
