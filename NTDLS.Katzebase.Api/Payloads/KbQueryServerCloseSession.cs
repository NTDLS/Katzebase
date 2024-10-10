using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
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
