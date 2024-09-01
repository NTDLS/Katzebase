using ParserV2.Expression.Expressions.Function;

namespace ParserV2.Expression.Expressions.Fields.Evaluation
{
    /// <summary>
    /// Contains a string evaluation expression. This could be as simple as ["This" + "That"] or could contain function calls which child nodes.
    /// </summary>
    internal class ExpressionStringEvaluation : IExpressionEvaluation
    {
        private int _nextExpressionKey = 0;

        public string Expression { get; set; } = string.Empty;

        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        public List<FunctionCallEvaluation> FunctionCalls { get; set; } = new();

        /// <summary>
        /// List of functions that are referenced by this expression. Just their names, keys and types (no parameters).
        /// </summary>
        public List<ReferencedFunction> ReferencedFunctions { get; private set; } = new();

        public ExpressionStringEvaluation()
        {
            //Expression = expression;
        }

        public string GetKeyExpressionKey()
            => $"$x_{_nextExpressionKey++}$";
    }
}
