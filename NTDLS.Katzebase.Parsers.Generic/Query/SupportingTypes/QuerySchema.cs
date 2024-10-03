using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class QuerySchema<TData> where TData : IStringable
    {
        public string Name { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public ConditionCollection<TData>? Conditions { get; set; }

        public QuerySchema(string name, string prefix, ConditionCollection<TData> conditions)
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
