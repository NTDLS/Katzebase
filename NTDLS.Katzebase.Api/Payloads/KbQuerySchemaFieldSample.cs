using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQuerySchemaFieldSample : IRmQuery<KbQuerySchemaFieldSampleReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }

        public KbQuerySchemaFieldSample(Guid connectionId, string schema)
        {
            ConnectionId = connectionId;
            Schema = schema;
        }
    }

    public class KbQuerySchemaFieldSampleReply : KbResponseFieldSampleCollection, IRmQueryReply
    {
    }
}
