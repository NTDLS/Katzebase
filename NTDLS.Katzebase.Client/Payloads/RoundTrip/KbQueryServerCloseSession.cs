using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQueryServerCloseSession : IRmQuery<KbQueryServerCloseSessionReply>
    {
        public Guid ConnectionId { get; set; }

        public KbQueryServerCloseSession(Guid connectionId)
        {
            ConnectionId = connectionId;
        }
    }

    public class KbQueryServerCloseSessionReply : KbBaseActionResponse, IRmQueryReply
    {
    }
}
