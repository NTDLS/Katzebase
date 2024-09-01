using ParserV2.Parsers.Query.Expressions.Function;

namespace ParserV2.Parsers.Query.Expressions.Fields.Evaluation
{
    internal class FunctionCallEvaluation
    {
        public string FunctionName { get; set; }
        public string ExpressionKey { get; set; }

        /// <summary>
        /// List of functions that are referenced by this expression. Just their names, keys and types (no parameters).
        /// </summary>
        public List<ReferencedFunction> ReferencedFunctions { get; private set; } = new();

        /// <summary>
        /// Parameter list for the this function.
        /// </summary>
        public List<IExpressionFunctionParameter> Parameters { get; set; } = new();

        public FunctionCallEvaluation(string functionName, string expressionKey)
        {
            FunctionName = functionName;
            ExpressionKey = expressionKey;
        }
    }
}
