using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
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
