using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;

namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class QuerySchema
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
