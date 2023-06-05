using Katzebase.Engine.Query.Condition;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Query
{
    public class PreparedQuery
    {
        public string Schema { get; set; } = string.Empty;
        public int RowLimit { get; set; }
        public QueryType QueryType { get; set; }
        //public Conditions Conditions { get; set; } = new();
        public Conditions Conditions { get; set; } = new();
        public UpsertKeyValues UpsertKeyValuePairs { get; set; } = new();
        public List<string> SelectFields { get; set; } = new();
    }
}
