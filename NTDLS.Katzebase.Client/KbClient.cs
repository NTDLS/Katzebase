using NTDLS.Katzebase.Client.Management;

namespace NTDLS.Katzebase.Client
{
    public class KbClient : IDisposable
    {
        public HttpClient Connection { get; private set; }
        public Guid SessionId { get; private set; }
        public KbDocumentClient Document { get; private set; }
        public KbSchemaClient Schema { get; private set; }
        public KbServerClient Server { get; private set; }
        public KbTransactionClient Transaction { get; private set; }
        public KbQueryClient Query { get; private set; }
        public KbProcedureClient Procedure { get; private set; }

        private readonly Thread _keepAliveThread;
        public string ClientName { get; private set; }

        internal object PingLock { get; private set; } = new();

        /// <summary>
        /// This is the process id of the session on the server. This is populated with each call to Client.Server.Ping().
        /// </summary>
        public ulong ServerProcessId { get; internal set; }

        /// <summary>
        /// Connects to the server using a URL.
        /// </summary>
        /// <param name="baseAddress">Base address should be in the form http://host:port/</param>
        public KbClient(string baseAddress, string clientName = "")
        {
            ClientName = clientName;

            SessionId = Guid.NewGuid();
            Connection = new HttpClient
            {
                BaseAddress = new Uri(baseAddress),
                Timeout = new TimeSpan(0, 8, 0, 0, 0)
            };

            Document = new KbDocumentClient(this);
            Schema = new KbSchemaClient(this);
            Server = new KbServerClient(this);
            Transaction = new KbTransactionClient(this);
            Query = new KbQueryClient(this);
            Procedure = new KbProcedureClient(this);

            Server.Ping();

            _keepAliveThread = new Thread(KeepAliveThread);
            _keepAliveThread.Start();
        }

        /// <summary>
        /// Connects to the server using a URL and a non-default timeout.
        /// </summary>
        /// <param name="baseAddress">Base address should be in the form http://host:port/</param>
        public KbClient(string baseAddress, TimeSpan timeout, string clientName = "")
        {
            ClientName = clientName;

            SessionId = Guid.NewGuid();
            Connection = new HttpClient
            {
                BaseAddress = new Uri(baseAddress),
                Timeout = timeout
            };

            Document = new KbDocumentClient(this);
            Schema = new KbSchemaClient(this);
            Server = new KbServerClient(this);
            Transaction = new KbTransactionClient(this);
            Query = new KbQueryClient(this);
            Procedure = new KbProcedureClient(this);

            Server.Ping();

            _keepAliveThread = new Thread(KeepAliveThread);
            _keepAliveThread.Start();
        }

        private void KeepAliveThread()
        {
            int approximateSleepTimeMs = 1000;

            while (disposed == false)
            {
                for (int sleep = 0; disposed == false && sleep < approximateSleepTimeMs + 10; sleep++)
                {
                    Thread.Sleep(10);
                }

                if (disposed == false)
                {
                    lock (PingLock)
                    {
                        try
                        {
                            Server.Ping(); //This keeps the connection alive on the server side.
                        }
                        catch { }
                    }
                }
            }
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
            lock (PingLock)
            {
                if (disposed)
                {
                    return;
                }

                if (disposing)
                {
                    if (Connection != null)
                    {
                        try
                        {
                            if (ServerProcessId != 0) //We have a ServerProcessId if we have ever had a successful ping.
                            {
                                Server.CloseSession();
                            }
                        }
                        catch
                        {
                        }
                        Connection.Dispose();
                    }
                }

                disposed = true;
            }
        }

        #endregion
    }
}
