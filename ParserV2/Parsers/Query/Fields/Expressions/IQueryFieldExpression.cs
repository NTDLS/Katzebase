using ParserV2.Parsers.Query.Functions;

namespace ParserV2.Parsers.Query.Fields.Expressions
{
    internal interface IQueryFieldExpression : IQueryField
    {
        string Expression { get; set; }
        string GetKeyExpressionKey();

        /// <summary>
        /// Contains the function names and their parameters that are used to satisfy the expression,
        /// </summary>
        List<QueryFieldExpressionFunction> FunctionCalls { get; }

        /// <summary>
        /// List of functions that are referenced by this expression. Just their names, keys and types (no parameters).
        /// </summary>
        List<FunctionReference> ReferencedFunctions { get; }
    }
}
