namespace ParserV2.Expression
{
    internal class FunctionCallEvaluation
    {
        public string FunctionName { get; set; }
        public string ExpressionKey { get; set; }

        /// <summary>
        /// Contains the keys of the parameters that must be satisfied by function calls.
        /// </summary>
        public List<string> FunctionDependencies { get; set; } = new();

        public List<IExpressionFunctionParameter> Parameters { get; set; } = new();

        public FunctionCallEvaluation(string functionName, string expressionKey)
        {
            FunctionName = functionName;
            ExpressionKey = expressionKey;
        }
    }
}
