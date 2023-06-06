using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query.Condition;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Query
{
    public class PreparedQuery
    {
        public List<QuerySchema> Schemas { get; set; } = new();
        public int RowLimit { get; set; }
        public QueryType QueryType { get; set; }
        public Conditions Conditions { get; set; } = new();
        public UpsertKeyValues UpsertKeyValuePairs { get; set; } = new();
        public List<QueryField> SelectFields { get; set; } = new();
        public List<KbNameValuePair> VariableValues { get; set; } = new();
    }
}
