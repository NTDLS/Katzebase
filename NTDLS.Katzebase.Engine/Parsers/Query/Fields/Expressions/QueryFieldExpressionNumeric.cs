namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions
{
    /// <summary>
    /// Contains a numeric evaluation expression. This could be as simple as [10 + 10] or could contain function calls which child nodes.
    /// </summary>
    public class QueryFieldExpressionNumeric : IQueryFieldExpression
    {
        private int _nextExpressionKey = 0;

        public string Value { get; set; }

        /// <summary>
        /// Not applicable to IQueryFieldExpression
        /// </summary>
        public string SchemaAlias { get; private set; } = string.Empty;

        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        public List<IQueryFieldExpressionFunction> FunctionDependencies { get; private set; } = new();

        public QueryFieldExpressionNumeric(string value)
        {
            Value = value;
        }

        public string GetKeyExpressionKey()
            => $"$x_{_nextExpressionKey++}$";
    }
}
