using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQueryDocumentCatalog
        : IRmQuery<KbQueryDocumentCatalogReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }

        public KbQueryDocumentCatalog(Guid connectionId, string schema)
        {
            ConnectionId = connectionId;
            Schema = schema;
        }
    }

    public class KbQueryDocumentCatalogReply : KbDocumentCatalogCollection, IRmQueryReply
    {
    }
}
