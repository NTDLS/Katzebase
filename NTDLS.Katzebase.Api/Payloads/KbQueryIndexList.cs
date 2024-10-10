using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQueryIndexList : IRmQuery<KbQueryIndexListReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }

        public KbQueryIndexList(Guid connectionId, string schema)
        {
            ConnectionId = connectionId;
            Schema = schema;
        }
    }

    public class KbQueryIndexListReply : KbActionResponseIndexes, IRmQueryReply
    {
    }
}
