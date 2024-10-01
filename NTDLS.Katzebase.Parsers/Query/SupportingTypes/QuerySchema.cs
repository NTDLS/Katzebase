using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;

namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class QuerySchema
    {
        public string Name { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public ConditionCollection? Conditions { get; set; }

        public int? ScriptLine { get; set; }

        public QuerySchema(int? scriptLine, string name, string prefix, ConditionCollection conditions)
        {
            ScriptLine = scriptLine;
            Name = name;
            Prefix = prefix;
            Conditions = conditions;
        }

        public QuerySchema(int? scriptLine, string name, string prefix)
        {
            ScriptLine = scriptLine;
            Name = name;
            Prefix = prefix;
        }

        public QuerySchema(int? scriptLine, string name)
        {
            ScriptLine = scriptLine;
            Name = name;
        }
    }
}
