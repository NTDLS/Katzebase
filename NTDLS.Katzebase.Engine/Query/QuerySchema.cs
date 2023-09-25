using Katzebase.Engine.Query.Constraints;

namespace Katzebase.Engine.Query
{
    internal class QuerySchema
    {
        public string Name { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public Conditions? Conditions { get; set; }

        public QuerySchema(string name, string prefix, Conditions conditions)
        {
            Name = name;
            Prefix = prefix;
            Conditions = conditions;
        }

        public QuerySchema(string name, string prefix)
        {
            Name = name;
            Prefix = prefix;
        }

        public QuerySchema(string name)
        {
            Name = name;
        }
    }
}
