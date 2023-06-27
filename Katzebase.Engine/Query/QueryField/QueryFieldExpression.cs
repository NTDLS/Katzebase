namespace Katzebase.Engine.Query.QueryField
{
    internal class QueryFieldExpression : QueryFieldParameterBase
    {
        public string Value { get; set; } = string.Empty;
        public List<QueryFieldParameterBase> Parameters { get; set; } = new();
    }
}
