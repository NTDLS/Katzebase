using Katzebase.Engine.Query.Constraints;

namespace Katzebase.Engine.Query
{
    internal class QuerySchema
    {
        public string Name { get; set; }
        public string Alias { get; set; } = string.Empty;
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
        public QuerySchema(string name)
        {
            Name = name;
        }
    }
}
