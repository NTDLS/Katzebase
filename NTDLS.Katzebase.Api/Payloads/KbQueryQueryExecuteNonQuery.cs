﻿using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Api.Types;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQueryQueryExecuteNonQuery : IRmQuery<KbQueryQueryExecuteNonQueryReply>
    {
        public Guid ConnectionId { get; set; }
        public string Statement { get; set; }
        public KbInsensitiveDictionary<KbVariable>? UserParameters { get; set; }

        public KbQueryQueryExecuteNonQuery(Guid connectionId, string statement, KbInsensitiveDictionary<KbVariable>? userParameters)
        {
            ConnectionId = connectionId;
            Statement = statement;
            UserParameters = userParameters;
        }
    }

    public class KbQueryQueryExecuteNonQueryReply : KbActionResponseCollection, IRmQueryReply
    {
    }
}
