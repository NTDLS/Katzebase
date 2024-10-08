﻿using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQueryIndexDrop : IRmQuery<KbQueryIndexDropReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }
        public string IndexName { get; set; }

        public KbQueryIndexDrop(Guid connectionId, string schema, string indexName)
        {
            ConnectionId = connectionId;
            Schema = schema;
            IndexName = indexName;
        }
    }

    public class KbQueryIndexDropReply : KbBaseActionResponse, IRmQueryReply
    {
    }
}
