namespace Katzebase.Engine.Query.QueryField
{
    internal class QueryFieldDocumentFieldParameter : QueryFieldParameterBase
    {
        public PrefixedField Value { get; set; }
        public string ExpressionKey { get; set; } = string.Empty;

        public QueryFieldDocumentFieldParameter(string value)
        {
            Value = PrefixedField.Parse(value);
        }
    }
}
