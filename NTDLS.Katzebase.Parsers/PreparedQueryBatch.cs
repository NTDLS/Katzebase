using NTDLS.Katzebase.Parsers.SupportingTypes;

namespace NTDLS.Katzebase.Parsers
{
    public class PreparedQueryBatch(QueryVariables variables)
        : List<PreparedQuery>
    {
        public QueryVariables Variables { get; set; } = variables;
    }
}
