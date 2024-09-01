using ParserV2.Expression.Expressions.Function;

namespace ParserV2.Expression.Expressions.Fields.Evaluation
{
    internal interface IExpressionEvaluation : IExpression
    {
        string Expression { get; set; }
        string GetKeyExpressionKey();

        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        List<FunctionCallEvaluation> FunctionCalls { get; }

        /// <summary>
        /// List of functions that are referenced by this expression. Just their names, keys and types (no parameters).
        /// </summary>
        List<ReferencedFunction> ReferencedFunctions { get; }
    }
}
