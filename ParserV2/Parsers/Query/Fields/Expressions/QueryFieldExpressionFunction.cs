using NTDLS.Katzebase.Client.Exceptions;
using ParserV2.Parsers.Query.Functions;
using static ParserV2.StandIn.Types;

namespace ParserV2.Parsers.Query.Fields.Expressions
{
    internal class QueryFieldExpressionFunction
    {
        public string FunctionName { get; set; }
        public string ExpressionKey { get; set; }
        public KbScalerFunctionParameterType ReturnType { get; set; }

        /// <summary>
        /// List of functions that are referenced by this expression. Just their names, keys and types (no parameters).
        /// </summary>
        public List<FunctionReference> ReferencedFunctions { get; private set; } = new();

        /// <summary>
        /// Parameter list for the this function.
        /// </summary>
        public List<IExpressionFunctionParameter> Parameters { get; set; } = new();


        public QueryFieldExpressionFunction(string functionName, string expressionKey, KbScalerFunctionParameterType returnType)
        {
            FunctionName = functionName;
            ExpressionKey = expressionKey;
            ReturnType = returnType;
        }
    }
}
