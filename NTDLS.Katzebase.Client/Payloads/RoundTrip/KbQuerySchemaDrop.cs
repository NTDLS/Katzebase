using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQuerySchemaDrop : IRmQuery<KbQuerySchemaDropReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }

        public KbQuerySchemaDrop(Guid connectionId, string schema)
        {
            ConnectionId = connectionId;
            Schema = schema;
        }
    }

    public class KbQuerySchemaDropReply : KbBaseActionResponse, IRmQueryReply
    {
    }
}
