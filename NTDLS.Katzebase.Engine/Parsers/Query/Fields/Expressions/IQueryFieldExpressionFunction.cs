using NTDLS.Katzebase.Engine.Parsers.Query.Functions;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Fields.Expressions
{
    public interface IQueryFieldExpressionFunction
    {
        string FunctionName { get; }
        string ExpressionKey { get; }
        BasicDataType ReturnType { get; }

        /// <summary>
        /// Parameter list for the this function.
        /// </summary>
        List<IExpressionFunctionParameter> Parameters { get; }
    }
}
