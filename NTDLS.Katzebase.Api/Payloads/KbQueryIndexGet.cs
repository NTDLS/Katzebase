using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQueryIndexGet : IRmQuery<KbQueryIndexGetReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }
        public string IndexName { get; set; }

        public KbQueryIndexGet(Guid connectionId, string schema, string indexName)
        {
            ConnectionId = connectionId;
            Schema = schema;
            IndexName = indexName;
        }
    }

    public class KbQueryIndexGetReply : KbActionResponseIndex, IRmQueryReply
    {
        public KbQueryIndexGetReply()
        {
        }

        public KbQueryIndexGetReply(KbIndex? index) : base(index) { }
    }
}
