namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class PreparedQueryBatch(QueryVariables variables)
        : List<PreparedQuery>
    {
        public QueryVariables Variables { get; set; } = variables;
    }
}
