namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class QueryBatch(QueryVariables variables)
        : List<Query>
    {
        public QueryVariables Variables { get; set; } = variables;
    }
}
