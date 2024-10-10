using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQueryDocumentList : IRmQuery<KbQueryDocumentListReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }

        public int Count { get; set; }

        public KbQueryDocumentList(Guid connectionId, string schema, int count)
        {
            ConnectionId = connectionId;
            Schema = schema;
            Count = count;
        }
    }

    public class KbQueryDocumentListReply : KbQueryResult, IRmQueryReply
    {
    }
}
