using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQueryServerTerminateProcess : IRmQuery<KbQueryServerTerminateProcessReply>
    {
        public Guid ConnectionId { get; set; }
        public ulong ReferencedProcessId { get; set; }

        public KbQueryServerTerminateProcess(Guid connectionId, ulong referencedProcessId)
        {
            ConnectionId = connectionId;
            ReferencedProcessId = referencedProcessId;
        }
    }

    public class KbQueryServerTerminateProcessReply : KbBaseActionResponse, IRmQueryReply
    {
    }
}
