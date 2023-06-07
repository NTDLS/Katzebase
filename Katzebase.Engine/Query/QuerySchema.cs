using Katzebase.Engine.Query.Condition;
using System.Xml.Linq;

namespace Katzebase.Engine.Query
{
    public class QuerySchema
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public Conditions? Conditions { get; set; }

        public QuerySchema(string name, string alias, Conditions conditions)
        {
            Name = name;
            Alias = alias;
            Conditions = conditions;
        }
        public QuerySchema(string name, string alias)
        {
            Name = name;
            Alias = alias;
        }
    }
}
