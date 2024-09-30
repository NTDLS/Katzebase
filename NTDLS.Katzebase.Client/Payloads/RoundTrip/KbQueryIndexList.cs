using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQueryIndexList : IRmQuery<KbQueryIndexListReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }

        public KbQueryIndexList(Guid connectionId, string schema)
        {
            ConnectionId = connectionId;
            Schema = schema;
        }
    }

    public class KbQueryIndexListReply : KbActionResponseIndexes, IRmQueryReply
    {
    }
}
