using NTDLS.Katzebase.Client.Types;
using NTDLS.ReliableMessaging;
using System.Xml.Linq;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQueryQueryExecuteQueries : IRmQuery<KbQueryQueryExecuteQueriesReply>
    {
        public Guid ConnectionId { get; set; }
        public List<string> Statements { get; set; }
        public KbInsensitiveDictionary<KbConstant>? UserParameters { get; set; }

        public KbQueryQueryExecuteQueries(Guid connectionId, List<string> statements, KbInsensitiveDictionary<KbConstant>? userParameters)
        {
            ConnectionId = connectionId;
            Statements = statements;
            UserParameters = userParameters;
        }
    }
    public class KbQueryQueryExecuteQueries<TData> : IRmQuery<KbQueryQueryExecuteQueriesReply<TData>>
    {
        public Guid ConnectionId { get; set; }
        public List<string> Statements { get; set; }
        public KbInsensitiveDictionary<KbConstant<TData>>? UserParameters { get; set; }

        public KbQueryQueryExecuteQueries(Guid connectionId, List<string> statements, KbInsensitiveDictionary<KbConstant<TData>>? userParameters)
        {
            ConnectionId = connectionId;
            Statements = statements;
            UserParameters = userParameters;
        }
    }

    public class KbQueryQueryExecuteQueriesReply : KbQueryResultCollection, IRmQueryReply
    {
    }
    
    public class KbQueryQueryExecuteQueriesReply<TData> : KbQueryResultCollection<TData>, IRmQueryReply
    {
    }
}
