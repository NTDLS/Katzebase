using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Api.Types;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQueryQueryExplainPlan : IRmQuery<KbQueryQueryExplainPlanReply>
    {
        public Guid ConnectionId { get; set; }
        public string Statement { get; set; }
        public KbInsensitiveDictionary<KbVariable>? UserParameters { get; set; }

        public KbQueryQueryExplainPlan(Guid connectionId, string statement, KbInsensitiveDictionary<KbVariable>? userParameters)
        {
            ConnectionId = connectionId;
            Statement = statement;
            UserParameters = userParameters;
        }
    }

    public class KbQueryQueryExplainPlanReply : KbQueryExplainCollection, IRmQueryReply
    {
    }
}
