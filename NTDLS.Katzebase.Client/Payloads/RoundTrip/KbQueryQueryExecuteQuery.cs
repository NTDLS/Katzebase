using NTDLS.Katzebase.Client.Types;
using NTDLS.ReliableMessaging;
using System.Xml.Linq;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQueryQueryExecuteQuery : IRmQuery<KbQueryQueryExecuteQueryReply>
    {
        public Guid ConnectionId { get; set; }
        public string Statement { get; set; }
        public KbInsensitiveDictionary<KbConstant>? UserParameters { get; set; }

        public KbQueryQueryExecuteQuery(Guid connectionId, string statement, KbInsensitiveDictionary<KbConstant>? userParameters)
        {
            ConnectionId = connectionId;
            Statement = statement;
            UserParameters = userParameters;
        }
    }
    public class KbQueryQueryExecuteQuery<TData> : IRmQuery<KbQueryQueryExecuteQueryReply<TData>>
    {
        public Guid ConnectionId { get; set; }
        public string Statement { get; set; }
        public KbInsensitiveDictionary<KbConstant<TData>>? UserParameters { get; set; }

        public KbQueryQueryExecuteQuery(Guid connectionId, string statement, KbInsensitiveDictionary<KbConstant<TData>>? userParameters)
        {
            ConnectionId = connectionId;
            Statement = statement;
            UserParameters = userParameters;
        }
    }

    public class KbQueryQueryExecuteQueryReply : KbQueryResultCollection, IRmQueryReply
    {
    }
    public class KbQueryQueryExecuteQueryReply<TData> : KbQueryResultCollection<TData>, IRmQueryReply
    {
    }
}
