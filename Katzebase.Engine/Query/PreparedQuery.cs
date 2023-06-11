using Katzebase.Engine.KbLib;
using Katzebase.Engine.Query.Constraints;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Query
{
    internal class PreparedQuery
    {
        public List<QuerySchema> Schemas { get; set; } = new();
        public int RowLimit { get; set; }
        public QueryType QueryType { get; set; }
        public Conditions Conditions { get; set; } = new();
        public UpsertKeyValues UpsertKeyValuePairs { get; set; } = new();
        public List<QueryField> SelectFields { get; set; } = new();
        public List<KbNameValuePair> VariableValues { get; set; } = new();

        public void AddSelectField(string key, string schemaAlias, string alias)
        {
            SelectFields.Add(new QueryField(key, schemaAlias, alias, SelectFields.Count));
        }
    }
}
