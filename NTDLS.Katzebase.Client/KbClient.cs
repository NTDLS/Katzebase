using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Management;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.ReliableMessaging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace NTDLS.Katzebase.Client
{
    public class KbClient : IDisposable
    {
        public delegate void ConnectedEvent(KbClient sender, KbSessionInfo sessionInfo);
        public event ConnectedEvent? OnConnected;

        public delegate void DisconnectedEvent(KbClient sender, KbSessionInfo sessionInfo);
        public event DisconnectedEvent? OnDisconnected;

        public delegate void CommunicationExceptionEvent(KbClient sender, KbSessionInfo sessionInfo, Exception ex);
        public event CommunicationExceptionEvent? OnCommunicationException;

        private TimeSpan _queryTimeout = TimeSpan.FromSeconds(30);

        public TimeSpan QueryTimeout
        {
            get { return _queryTimeout; }
            set
            {
                _queryTimeout = value;
                if (Connection?.IsConnected == true)
                {
                    Connection.QueryTimeout = _queryTimeout;
                }
            }
        }

        internal RmClient? Connection { get; private set; }

        public bool IsConnected => Connection?.IsConnected == true;

        public string Host { get; private set; } = string.Empty;
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

        public KbClient()
        {
            Document = new KbDocumentClient(this);
            Schema = new KbSchemaClient(this);
            Server = new KbServerClient(this);
            Transaction = new KbTransactionClient(this);
            Query = new KbQueryClient(this);
            Procedure = new KbProcedureClient(this);
        }

        public KbClient(string hostName, int serverPort, string userName, string password, string clientName = "")
        {
            Document = new KbDocumentClient(this);
            Schema = new KbSchemaClient(this);
            Server = new KbServerClient(this);
            Transaction = new KbTransactionClient(this);
            Query = new KbQueryClient(this);
            Procedure = new KbProcedureClient(this);

            Connect(hostName, serverPort, userName, password, clientName);
        }

        /// <summary>
        /// Returns a SHA256 of the given string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string HashPassword(string input)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

            var builder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Connects to an instance of the server
        /// </summary>
        /// <param name="hostname">Host or ip of the server.</param>
        /// <param name="port">TCP/IP port of the server</param>
        /// <param name="username">Username to log in with.</param>
        /// <param name="passwordHash">SHA256 of the password for the given user.</param>
        /// <param name="clientName">Name of the client that is connecting to the server.</param>
        /// <exception cref="KbGenericException"></exception>
        public void Connect(string hostname, int port, string username, string passwordHash, string clientName)
        {
            Host = hostname;
            Port = port;
            Username = username;
            ClientName = clientName;

            if (string.IsNullOrWhiteSpace(clientName))
            {
                clientName = Process.GetCurrentProcess().ProcessName;
            }

            if (Connection?.IsConnected == true)
            {
                throw new KbGenericException("The client is already connected.");
            }

            try
            {
                Connection = new RmClient(new RmConfiguration
                {
                    QueryTimeout = _queryTimeout
                });

                Connection.OnException += (RmContext? context, Exception ex, IRmPayload? payload) =>
                {
                    var sessionInfo = new KbSessionInfo
                    {
                        ConnectionId = ServerConnectionId,
                        ProcessId = ProcessId
                    };

                    OnCommunicationException?.Invoke(this, sessionInfo, ex);
                };

                Connection.OnDisconnected += (RmContext context) =>
                {
                    var sessionInfo = new KbSessionInfo
                    {
                        ConnectionId = ServerConnectionId,
                        ProcessId = ProcessId
                    };

                    OnDisconnected?.Invoke(this, sessionInfo);

                    Connection = null;
                    ServerConnectionId = Guid.Empty;
                    ProcessId = 0;
                };

                Connection.Connect(hostname, port);

                var reply = Server.StartSession(username, passwordHash, clientName);
                ServerConnectionId = reply.ConnectionId;
                ProcessId = reply.ProcessId;

                var sessionInfo = new KbSessionInfo
                {
                    ConnectionId = ServerConnectionId,
                    ProcessId = ProcessId
                };
                OnConnected?.Invoke(this, sessionInfo);
            }
            catch
            {
                Connection = null;
                ServerConnectionId = Guid.Empty;
                throw;
            }
        }

        void Disconnect()
        {
            try
            {
                try
                {
                    if (Connection?.IsConnected == true)
                    {
                        Server.CloseSession();
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    Connection?.Disconnect();
                    Connection = null;
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                Connection = null;
                ServerConnectionId = Guid.Empty;
                ProcessId = 0;
            }
        }

        internal T ValidateTaskResult<T>(Task<T> task)
        {
            if (task.IsCompletedSuccessfully == false)
            {
                throw new KbAPIResponseException(task.Exception?.GetRoot()?.Message ?? "Unspecified api error has occurred.");
            }
            return task.Result;
        }

        #region IDisposable.

        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                Disconnect();
            }

            disposed = true;

        }

        #endregion
    }
}
