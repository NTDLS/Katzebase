using NTDLS.Katzebase.Parsers.Query.Functions;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Parsers.Query.Fields.Expressions
{
    public class QueryFieldExpressionFunctionScalar : IQueryFieldExpressionFunction
    {
        public string FunctionName { get; set; }
        public string ExpressionKey { get; set; }
        public KbBasicDataType ReturnType { get; set; }

        /// <summary>
        /// Parameter list for the this function.
        /// </summary>
        public List<IExpressionFunctionParameter> Parameters { get; set; } = new();

        public QueryFieldExpressionFunctionScalar(string functionName, string expressionKey, KbBasicDataType returnType)
        {
            FunctionName = functionName;
            ExpressionKey = expressionKey;
            ReturnType = returnType;
        }

        public IQueryFieldExpressionFunction Clone()
        {
            var clone = new QueryFieldExpressionFunctionScalar(FunctionName, ExpressionKey, ReturnType);

            foreach (var parameter in Parameters)
            {
                clone.Parameters.Add(parameter.Clone());
            }

            return clone;
        }
    }
}
