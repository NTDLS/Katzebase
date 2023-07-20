namespace Katzebase.PublicLibrary.Client
{
    public class KatzebaseClient : IDisposable
    {
        public HttpClient Connection { get; private set; }
        public Guid SessionId { get; private set; }
        public Management.Document Document { get; private set; }
        public Management.Schema Schema { get; private set; }
        public Management.Server Server { get; private set; }
        public Management.Transaction Transaction { get; private set; }
        public Management.Query Query { get; private set; }

        private readonly Thread keepAliveThread;

        /// <summary>
        /// This is the process id of the session on the server. This is populated with each call to Client.Server.Ping().
        /// </summary>
        public ulong ServerProcessId { get; set; }

        public KatzebaseClient(string baseAddress)
        {
            SessionId = Guid.NewGuid();
            Connection = new HttpClient
            {
                BaseAddress = new Uri(baseAddress),
                Timeout = new TimeSpan(0, 8, 0, 0, 0)
            };

            Document = new Management.Document(this);
            Schema = new Management.Schema(this);
            Server = new Management.Server(this);
            Transaction = new Management.Transaction(this);
            Query = new Management.Query(this);

            keepAliveThread = new Thread(KeepAliveThread);
            keepAliveThread.Start();
        }

        public KatzebaseClient(string baseAddress, TimeSpan timeout)
        {
            SessionId = Guid.NewGuid();
            Connection = new HttpClient
            {
                BaseAddress = new Uri(baseAddress),
                Timeout = timeout
            };

            Document = new Management.Document(this);
            Schema = new Management.Schema(this);
            Server = new Management.Server(this);
            Transaction = new Management.Transaction(this);
            Query = new Management.Query(this);

            keepAliveThread = new Thread(KeepAliveThread);
            keepAliveThread.Start();
        }

        private void KeepAliveThread()
        {
            int approximateSleepTimeMs = 1000;

            while (disposed == false)
            {
                try
                {
                    Server.Ping(); //This keeps the connection alive on the server side.
                }
                catch
                {
                }

                for (int sleep = 0; disposed == false && sleep < (approximateSleepTimeMs + 10); sleep++)
                {
                    Thread.Sleep(10);
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
                            Transaction.Rollback();
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

        #endregion
    }
}
