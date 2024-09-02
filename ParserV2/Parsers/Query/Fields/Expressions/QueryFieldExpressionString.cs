namespace ParserV2.Parsers.Query.Fields.Expressions
{
    /// <summary>
    /// Contains a string evaluation expression. This could be as simple as ["This" + "That"] or could contain function calls which child nodes.
    /// </summary>
    internal class QueryFieldExpressionString : IQueryFieldExpression
    {
        private int _nextExpressionKey = 0;

        public string Expression { get; set; } = string.Empty;

        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        public List<QueryFieldExpressionFunction> FunctionDependencies { get; set; } = new();

        public QueryFieldExpressionString()
        {
            //Expression = expression;
        }

        public string GetKeyExpressionKey()
            => $"$x_{_nextExpressionKey++}$";
    }
}
