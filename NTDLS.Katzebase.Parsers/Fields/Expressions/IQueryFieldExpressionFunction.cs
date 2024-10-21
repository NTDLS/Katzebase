using NTDLS.Katzebase.Parsers.Functions;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Parsers.Fields.Expressions
{
    public interface IQueryFieldExpressionFunction
    {
        string FunctionName { get; }
        string ExpressionKey { get; }
        KbBasicDataType ReturnType { get; }

        /// <summary>
        /// Parameter list for the this function.
        /// </summary>
        List<IExpressionFunctionParameter> Parameters { get; }
        public IQueryFieldExpressionFunction Clone();
    }
}
