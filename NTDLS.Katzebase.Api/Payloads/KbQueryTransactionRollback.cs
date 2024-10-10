using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQueryTransactionRollback : IRmQuery<KbQueryTransactionRollbackReply>
    {
        public Guid ConnectionId { get; set; }

        public KbQueryTransactionRollback(Guid connectionId)
        {
            ConnectionId = connectionId;
        }
    }

    public class KbQueryTransactionRollbackReply : KbBaseActionResponse, IRmQueryReply
    {
    }
}
