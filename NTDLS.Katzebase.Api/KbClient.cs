using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Management;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads;
using NTDLS.ReliableMessaging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace NTDLS.Katzebase.Api
{
    /// <summary>
    /// A connection (and session) to a Katzebase server.
    ///
    /// A client holds a single server session, and a session has at most one open transaction: every request made through
    /// the client, from any thread, participates in that transaction. Use a separate client per independent unit of work.
    ///
    /// All requests throw Katzebase exceptions (see NTDLS.Katzebase.Api.Exceptions): the same exception type that the server
    /// threw (e.g. <see cref="KbDuplicateKeyViolationException"/>, <see cref="KbObjectNotFoundException"/>,
    /// <see cref="KbDeadlockException"/>, <see cref="KbParserException"/>), <see cref="KbTimeoutException"/> when the server does
    /// not reply in time, or <see cref="KbConnectionException"/> when the client is not (or is no longer) connected.
    /// </summary>
    public class KbClient : IDisposable
    {
        public delegate void ConnectedEvent(KbClient sender, KbSessionInfo sessionInfo);
        public event ConnectedEvent? OnConnected;

        public delegate void DisconnectedEvent(KbClient sender, KbSessionInfo sessionInfo);
        public event DisconnectedEvent? OnDisconnected;

        public delegate void CommunicationExceptionEvent(KbClient sender, KbSessionInfo sessionInfo, Exception ex);
        public event CommunicationExceptionEvent? OnCommunicationException;

        private TimeSpan _queryTimeout = TimeSpan.FromSeconds(30);
        private Timer? _heartbeatTimer;
        private readonly Lock _connectionLock = new();

        /// <summary>
        /// The default amount of time to wait for the server to reply to a request. Every request method also accepts its own timeout.
        /// </summary>
        public TimeSpan QueryTimeout
        {
            get => _queryTimeout;
            set
            {
                _queryTimeout = value;
                var connection = Connection;
                if (connection != null)
                {
                    connection.QueryTimeout = value;
                }
            }
        }

        internal RmClient? Connection { get; private set; }

        public bool IsConnected => Connection?.IsConnected == true;

        public string Address { get; private set; } = string.Empty;
        public int Port { get; private set; }
        public ulong ProcessId { get; private set; }
        public string Username { get; private set; } = string.Empty;
        public string ClientName { get; private set; } = string.Empty;
        public Guid ServerConnectionId { get; private set; }

        public KbDocumentClient Document { get; private set; }
        public KbSchemaClient Schema { get; private set; }
        public KbServerClient Server { get; private set; }
        public KbTransactionClient Transaction { get; private set; }
        public KbQueryClient Query { get; private set; }
        public KbProcedureClient Procedure { get; private set; }

        /// <summary>
        /// Creates a client that is not yet connected, see <see cref="Connect"/>.
        /// </summary>
        public KbClient()
        {
            Document = new KbDocumentClient(this);
            Schema = new KbSchemaClient(this);
            Server = new KbServerClient(this);
            Transaction = new KbTransactionClient(this);
            Query = new KbQueryClient(this);
            Procedure = new KbProcedureClient(this);
        }

        /// <summary>
        /// Creates a client and connects to the server.
        /// </summary>
        /// <param name="passwordHash">The SHA256 hash of the password, see <see cref="HashPassword(string)"/>.</param>
        public KbClient(string serverAddress, int serverPort, string userName, string passwordHash, string clientName = "")
            : this()
        {
            Connect(serverAddress, serverPort, userName, passwordHash, clientName);
        }

        /// <summary>
        /// Returns the SHA256 hash of the given password, which is what the server expects at login.
        /// </summary>
        public static string HashPassword(string password)
            => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

        #region Connection.

        /// <summary>
        /// Connects to an instance of the server and starts a session.
        /// </summary>
        /// <param name="hostname">Host or ip of the server.</param>
        /// <param name="port">TCP/IP port of the server</param>
        /// <param name="username">Username to log in with.</param>
        /// <param name="passwordHash">SHA256 of the password for the given user, see <see cref="HashPassword(string)"/>.</param>
        /// <param name="clientName">Name of the client that is connecting to the server (shown in the server's process list).</param>
        public void Connect(string hostname, int port, string username, string passwordHash, string clientName = "")
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (string.IsNullOrWhiteSpace(clientName))
            {
                clientName = Process.GetCurrentProcess().ProcessName;
            }

            RmClient connection;
            lock (_connectionLock)
            {
                if (Connection?.IsConnected == true)
                {
                    throw new KbConnectionException("The client is already connected.");
                }

                Address = hostname;
                Port = port;
                Username = username;
                ClientName = clientName;

                connection = new RmClient(new RmConfiguration
                {
                    QueryTimeout = _queryTimeout
                });

                connection.OnException += (RmContext? context, Exception ex, IRmPayload? payload) =>
                {
                    OnCommunicationException?.Invoke(this, CurrentSessionInfo(), ex);
                };

                connection.OnDisconnected += (RmContext context) =>
                {
                    var sessionInfo = CurrentSessionInfo();
                    ResetConnectionState(connection);
                    OnDisconnected?.Invoke(this, sessionInfo);
                };

                Connection = connection;
            }

            try
            {
                connection.Connect(hostname, port);

                var reply = Server.StartSession(username, passwordHash, clientName);
                ServerConnectionId = reply.ConnectionId;
                ProcessId = reply.ProcessId;

                _heartbeatTimer = new Timer(_ => SendHeartbeat(), null,
                    TimeSpan.FromSeconds(KbConstants.HeartbeatSeconds), TimeSpan.FromSeconds(KbConstants.HeartbeatSeconds));
            }
            catch (Exception ex)
            {
                try { connection.Disconnect(); } catch { }
                ResetConnectionState(connection);

                throw ex is KbExceptionBase ? ex : new KbConnectionException($"Failed to connect to [{hostname}:{port}]: {ex.Message}", ex);
            }

            OnConnected?.Invoke(this, CurrentSessionInfo());
        }

        /// <summary>
        /// Ends the session (rolling back any open transaction) and disconnects from the server. Safe to call when not connected.
        /// </summary>
        public void Disconnect()
        {
            RmClient? connection;
            lock (_connectionLock)
            {
                connection = Connection;
            }

            if (connection == null)
            {
                return;
            }

            try
            {
                if (connection.IsConnected)
                {
                    Server.CloseSession();
                }
            }
            catch
            {
                //The server ends the session itself when the connection closes.
            }
            finally
            {
                try { connection.Disconnect(); } catch { }
                ResetConnectionState(connection);
            }
        }

        private void SendHeartbeat()
        {
            try
            {
                var connection = Connection;
                if (connection?.IsConnected == true)
                {
                    connection.Notify(new KbNotifySessionHeartbeat());
                }
            }
            catch
            {
                //A failed heartbeat surfaces as a disconnect.
            }
        }

        private void ResetConnectionState(RmClient connection)
        {
            lock (_connectionLock)
            {
                if (Connection != connection)
                {
                    return; //A newer connection has already been established.
                }

                _heartbeatTimer?.Dispose();
                _heartbeatTimer = null;
                Connection = null;
                ServerConnectionId = Guid.Empty;
                ProcessId = 0;
            }
        }

        private KbSessionInfo CurrentSessionInfo()
            => new() { ConnectionId = ServerConnectionId, ProcessId = ProcessId };

        #endregion

        #region Request execution.

        private RmClient GetConnection()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var connection = Connection;
            if (connection?.IsConnected != true)
            {
                throw new KbConnectionException("The client is not connected.");
            }
            return connection;
        }

        /// <summary>
        /// Sends a request to the server and waits for the reply, translating failures into Katzebase exceptions.
        /// </summary>
        internal TReply Send<TReply>(IRmQuery<TReply> query, TimeSpan? queryTimeout)
            where TReply : IRmQueryReply
        {
            var connection = GetConnection();
            try
            {
                return connection.Query(query, queryTimeout ?? _queryTimeout);
            }
            catch (Exception ex)
            {
                throw KbRemoteExceptionMapper.Map(ex, connection.IsConnected);
            }
        }

        /// <summary>
        /// Sends a request to the server and asynchronously waits for the reply, translating failures into Katzebase exceptions.
        /// </summary>
        internal async Task<TReply> SendAsync<TReply>(IRmQuery<TReply> query, TimeSpan? queryTimeout, CancellationToken cancellationToken)
            where TReply : IRmQueryReply
        {
            var connection = GetConnection();
            try
            {
                return await connection.QueryAsync(query, queryTimeout ?? _queryTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw KbRemoteExceptionMapper.Map(ex, connection.IsConnected);
            }
        }

        #endregion

        #region IDisposable.

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Disconnect(); //Never throws.
            }

            _disposed = true;
        }

        #endregion
    }
}
