using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using static NTDLS.Katzebase.Engine.Functions.Parameters.FunctionParameterTypes;

namespace NTDLS.Katzebase.Engine.Functions.Parameters
{
    internal class FunctionWithParams : FunctionParameterBase
    {

        public string Function { get; private set; } = string.Empty;
        public List<FunctionParameterBase> Parameters { get; private set; } = new();

        public FunctionType FunctionType { get; set; }

        public T? GetParam<T>(int ordinal)
        {
            if (ordinal >= Parameters.Count)
            {
                throw new KbFunctionException($"Parameter at ordinal [{ordinal}] was not passed to function.");
            }

            if (Parameters[ordinal] is not FunctionExpression expression)
            {
                throw new KbFunctionException($"Parameter at ordinal [{ordinal}] could not be converted to an expression.");
            }

            return Converters.ConvertTo<T?>(expression.Value);
        }

        public FunctionWithParams(string functionName)
        {
            Function = functionName;

            if (Old_AggregateFunctionNames.Contains(Function.ToLowerInvariant()))
            {
                FunctionType = FunctionType.Aggregate;
            }
            else
            {
                FunctionType = FunctionType.Scaler;
            }
        }
    }
}
