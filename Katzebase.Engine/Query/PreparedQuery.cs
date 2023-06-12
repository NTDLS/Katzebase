using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Query.Tokenizers;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Query
{
    internal class PreparedQuery
    {
        public List<QuerySchema> Schemas { get; set; } = new();
        public int RowLimit { get; set; }
        public QueryType QueryType { get; set; }
        public SubQueryType SubQueryType { get; set; }
        public Conditions Conditions { get; set; } = new();
        public PrefixedFields SelectFields { get; set; } = new();
        public PrefixedFields GroupFields { get; set; } = new();
        public SortFields SortFields { get; set; } = new();
        public UpsertKeyValues UpsertKeyValuePairs { get; set; } = new();
        public List<KbNameValuePair> VariableValues { get; set; } = new();
    }
}
