namespace Katzebase.PublicLibrary.Client
{
    public class KatzebaseClient
    {
        public HttpClient Connection { get; private set; }
        public Guid SessionId { get; private set; }
        public Management.Document Document { get; private set; }
        public Management.Schema Schema { get; private set; }
        public Management.Server Server { get; private set; }
        public Management.Transaction Transaction { get; private set; }
        public Management.Query Query { get; private set; }

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
        }
    }
}
