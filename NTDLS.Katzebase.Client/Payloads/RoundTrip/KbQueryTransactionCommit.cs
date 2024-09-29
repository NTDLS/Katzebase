using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQueryTransactionCommit : IRmQuery<KbQueryTransactionCommitReply>
    {
        public Guid ConnectionId { get; set; }

        public KbQueryTransactionCommit(Guid connectionId)
        {
            ConnectionId = connectionId;
        }
    }

    public class KbQueryTransactionCommitReply : KbBaseActionResponse, IRmQueryReply
    {
    }
}
