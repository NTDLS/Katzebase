using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
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
