using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
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
