namespace Katzebase.Engine.Query.QueryField
{
    internal class QueryFieldConstantParameter : QueryFieldParameterBase
    {
        public string Value { get; set; } = string.Empty;

        public QueryFieldConstantParameter(string value)
        {
            Value = value;
        }
    }
}
