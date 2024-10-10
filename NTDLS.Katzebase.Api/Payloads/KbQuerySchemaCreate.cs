using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQuerySchemaCreate : IRmQuery<KbQuerySchemaCreateReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }
        public uint PageSize { get; set; }

        public KbQuerySchemaCreate(Guid connectionId, string schema, uint pageSize)
        {
            ConnectionId = connectionId;
            Schema = schema;
            PageSize = pageSize;
        }
    }

    public class KbQuerySchemaCreateReply : KbActionResponseBoolean, IRmQueryReply
    {
    }
}
