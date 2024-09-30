using NTDLS.Katzebase.Parsers.Query.Functions;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Parsers.Query.Fields.Expressions
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
