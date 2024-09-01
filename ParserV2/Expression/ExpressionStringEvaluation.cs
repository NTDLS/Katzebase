namespace ParserV2.Expression
{
    /// <summary>
    /// Contains a string evaluation expression. This could be as simple as ["This" + "That"] or could contain function calls which child nodes.
    /// </summary>
    internal class ExpressionStringEvaluation : IExpressionEvaluation
    {
        private int _nextExpressionKey = 0;

        public string Expression { get; set; } = string.Empty;
        public List<FunctionCallEvaluation> FunctionCalls { get; set; } = new();
        public List<ReferencedFunction> ReferencedFunctions { get; private set; } = new();

        public ExpressionStringEvaluation()
        {
            //Expression = expression;
        }

        public string GetKeyExpressionKey()
            => $"#x_{_nextExpressionKey++}#";
    }
}
