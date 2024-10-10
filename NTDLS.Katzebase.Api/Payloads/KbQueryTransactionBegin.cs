using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQueryTransactionBegin : IRmQuery<KbQueryTransactionBeginReply>
    {
        public Guid ConnectionId { get; set; }

        public KbQueryTransactionBegin(Guid connectionId)
        {
            ConnectionId = connectionId;
        }
    }

    public class KbQueryTransactionBeginReply : KbBaseActionResponse, IRmQueryReply
    {
    }
}
