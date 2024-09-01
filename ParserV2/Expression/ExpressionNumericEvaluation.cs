namespace ParserV2.Expression
{
    /// <summary>
    /// Contains a numeric evaluation expression. This could be as simple as [10 + 10] or could contain function calls which child nodes.
    /// </summary>
    internal class ExpressionNumericEvaluation : IExpressionEvaluation
    {
        private int _nextExpressionKey = 0;

        public string Expression { get; set; }
        public List<FunctionCallEvaluation> FunctionCalls { get; private set; } = new();
        public List<ReferencedFunction> ReferencedFunctions { get; private set; } = new();

        public ExpressionNumericEvaluation(string expression)
        {
            Expression = expression;
        }

        public string GetKeyExpressionKey()
            => $"$p_{_nextExpressionKey++}$";
    }
}
