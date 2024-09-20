using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;

namespace NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes
{
    internal class QuerySchema
    {
        public string Name { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public ConditionCollection? Conditions { get; set; }

        public QuerySchema(string name, string prefix, ConditionCollection conditions)
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
