using ParserV2.Expression.Expressions.Function;

namespace ParserV2.Expression.Expressions.Fields.Evaluation
{
    /// <summary>
    /// Contains a numeric evaluation expression. This could be as simple as [10 + 10] or could contain function calls which child nodes.
    /// </summary>
    internal class ExpressionNumericEvaluation : IExpressionEvaluation
    {
        private int _nextExpressionKey = 0;
        public string Expression { get; set; }

        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        public List<FunctionCallEvaluation> FunctionCalls { get; private set; } = new();

        /// <summary>
        /// List of functions that are referenced by this expression. Just their names, keys and types (no parameters).
        /// </summary>
        public List<ReferencedFunction> ReferencedFunctions { get; private set; } = new();

        public ExpressionNumericEvaluation(string expression)
        {
            Expression = expression;
        }

        public string GetKeyExpressionKey()
            => $"$p_{_nextExpressionKey++}$";
    }
}
