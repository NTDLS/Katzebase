using ParserV2.Parsers.Query.Functions;

namespace ParserV2.Parsers.Query.Fields.Expressions
{
    /// <summary>
    /// Contains a numeric evaluation expression. This could be as simple as [10 + 10] or could contain function calls which child nodes.
    /// </summary>
    internal class QueryFieldExpressionNumeric : IQueryFieldExpression
    {
        private int _nextExpressionKey = 0;

        public string Expression { get; set; }

        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        public List<QueryFieldExpressionFunction> FunctionCalls { get; private set; } = new();

        /// <summary>
        /// List of functions that are referenced by this expression. Just their names, keys and types (no parameters).
        /// </summary>
        public List<FunctionReference> ReferencedFunctions { get; private set; } = new();

        public QueryFieldExpressionNumeric(string expression)
        {
            Expression = expression;
        }

        public string GetKeyExpressionKey()
            => $"$x_{_nextExpressionKey++}$";
    }
}
