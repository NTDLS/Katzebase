using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQueryDocumentSample : IRmQuery<KbQueryDocumentSampleReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }

        public int Count { get; set; }

        public KbQueryDocumentSample(Guid connectionId, string schema, int count)
        {
            ConnectionId = connectionId;
            Schema = schema;
            Count = count;
        }
    }

    public class KbQueryDocumentSampleReply : KbQueryDocumentListResult, IRmQueryReply
    {
    }
}
