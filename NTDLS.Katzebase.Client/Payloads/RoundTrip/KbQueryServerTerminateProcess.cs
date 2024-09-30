using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
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
