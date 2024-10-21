using NTDLS.Katzebase.Parsers.Functions;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Parsers.Fields.Expressions
{
    public class QueryFieldExpressionFunctionAggregate : IQueryFieldExpressionFunction
    {
        public string FunctionName { get; set; }
        public string ExpressionKey { get; set; }
        public KbBasicDataType ReturnType { get; set; }

        /// <summary>
        /// Parameter list for the this function.
        /// </summary>
        public List<IExpressionFunctionParameter> Parameters { get; set; } = new();

        public QueryFieldExpressionFunctionAggregate(string functionName, string expressionKey, KbBasicDataType returnType)
        {
            FunctionName = functionName;
            ExpressionKey = expressionKey;
            ReturnType = returnType;
        }

        public IQueryFieldExpressionFunction Clone()
        {
            var clone = new QueryFieldExpressionFunctionAggregate(FunctionName, ExpressionKey, ReturnType);

            foreach (var parameter in Parameters)
            {
                clone.Parameters.Add(parameter.Clone());
            }

            return clone;
        }
    }
}
