using System;
using System.Net.Http;

namespace Katzebase.Library.Client
{
    public class KatzebaseClient
    {
        public HttpClient Client;
        public Guid SessionId { get; set; }
        public Management.Document Document { get; set; }
        public Management.Schema Schema { get; set; }
        public Management.Transaction Transaction { get; set; }

        public Management.Query Query { get; set; }

        public KatzebaseClient(string baseAddress)
        {
            Initialize(baseAddress, new TimeSpan(0, 8, 0, 0, 0));
        }

        public KatzebaseClient(string baseAddress, TimeSpan timeout)
        {
            Initialize(baseAddress, timeout);
        }

        private void Initialize(string baseAddress, TimeSpan timeout)
        {
            SessionId = Guid.NewGuid();
            Client = new HttpClient();
            Client.BaseAddress = new Uri(baseAddress);
            Client.Timeout = timeout;

            Document = new Management.Document(this);
            Schema = new Management.Schema(this);
            Transaction = new Management.Transaction(this);
            Query = new Management.Query(this);
        }

    }
}
