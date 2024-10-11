namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class QueryBatch(QueryVariables variables)
        : List<PreparedQuery>
    {
        public QueryVariables Variables { get; set; } = variables;
    }
}
