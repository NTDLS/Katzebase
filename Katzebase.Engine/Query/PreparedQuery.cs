using System.Collections.Generic;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class PreparedQuery
    {
        public string Schema { get; set; }
        public int RowLimit { get; set; }
        public QueryType QueryType { get; set; }
        public Conditions Conditions = new Conditions();
        public UpsertKeyValues UpsertKeyValuePairs { get; set; }
        public List<string> SelectFields = new List<string>();
    }
}
