using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQueryDocumentDeleteById : IRmQuery<KbQueryDocumentDeleteByIdReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }
        public uint Id { get; set; }

        public KbQueryDocumentDeleteById(Guid connectionId, string schema, uint id)
        {
            ConnectionId = connectionId;
            Schema = schema;
            Id = id;
        }
    }

    public class KbQueryDocumentDeleteByIdReply : KbBaseActionResponse, IRmQueryReply
    {
    }
}
