using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class PreparedQuery
    {
        public string Schema { get; set; } = string.Empty;
        public int RowLimit { get; set; }
        public QueryType QueryType { get; set; }
        public Conditions Conditions = new();
        public UpsertKeyValues UpsertKeyValuePairs { get; set; } = new();
        public List<string> SelectFields = new();
    }
}
