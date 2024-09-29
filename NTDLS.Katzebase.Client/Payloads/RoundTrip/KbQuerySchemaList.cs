using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQuerySchemaList : IRmQuery<KbQuerySchemaListReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }

        public KbQuerySchemaList(Guid connectionId, string schema)
        {
            ConnectionId = connectionId;
            Schema = schema;
        }
    }

    public class KbQuerySchemaListReply : KbActionResponseSchemaCollection, IRmQueryReply
    {
    }
}
