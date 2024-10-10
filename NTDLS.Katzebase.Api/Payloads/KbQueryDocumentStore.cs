using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQueryDocumentStore : IRmQuery<KbQueryDocumentStoreReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }
        public KbDocument Document { get; set; }

        public KbQueryDocumentStore(Guid connectionId, string schema, KbDocument document)
        {
            ConnectionId = connectionId;
            Schema = schema;
            Document = document;
        }
    }

    public class KbQueryDocumentStoreReply : KbActionResponseUInt, IRmQueryReply
    {
    }
}
