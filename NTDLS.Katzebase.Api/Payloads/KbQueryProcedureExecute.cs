using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbQueryProcedureExecute : IRmQuery<KbQueryProcedureExecuteReply>
    {
        public Guid ConnectionId { get; set; }
        public KbProcedure Procedure { get; set; }

        public KbQueryProcedureExecute(Guid connectionId, KbProcedure procedure)
        {
            ConnectionId = connectionId;
            Procedure = procedure;
        }
    }

    public class KbQueryProcedureExecuteReply : KbQueryResultCollection, IRmQueryReply
    {
    }
}
