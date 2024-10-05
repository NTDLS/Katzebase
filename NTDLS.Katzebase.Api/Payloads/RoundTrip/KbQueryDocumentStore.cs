using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads.RoundTrip
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
