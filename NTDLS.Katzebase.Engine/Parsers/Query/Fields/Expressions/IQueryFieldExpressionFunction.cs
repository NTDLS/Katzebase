using NTDLS.Katzebase.Engine.Parsers.Query.Functions;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions
{
    internal interface IQueryFieldExpressionFunction
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
