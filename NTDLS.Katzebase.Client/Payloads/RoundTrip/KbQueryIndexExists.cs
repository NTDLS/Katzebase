using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQueryIndexExists : IRmQuery<KbQueryIndexExistsReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }
        public string IndexName { get; set; }

        public KbQueryIndexExists(Guid connectionId, string schema, string indexName)
        {
            ConnectionId = connectionId;
            Schema = schema;
            IndexName = indexName;
        }
    }

    public class KbQueryIndexExistsReply : KbActionResponseBoolean, IRmQueryReply
    {
        public KbQueryIndexExistsReply()
        {

        }

        public KbQueryIndexExistsReply(bool value) : base(value) { }
    }
}
