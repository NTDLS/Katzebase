using Katzebase.Engine.Library;
using Katzebase.PublicLibrary.Exceptions;
using static Katzebase.Engine.Query.FunctionParameter.FunctionParameterTypes;

namespace Katzebase.Engine.Query.FunctionParameter
{
    internal class FunctionWithParams : FunctionParameterBase
    {

        public string Function { get; set; } = string.Empty;
        public List<FunctionParameterBase> Parameters { get; set; } = new();

        public FunctionType FunctionType { get; set; }

        public T? GetParam<T>(int ordinal)
        {
            if (ordinal >= Parameters.Count)
            {
                throw new KbFunctionException($"Parameter {ordinal} was not passed to function.");
            }

            var expression = Parameters[ordinal] as FunctionExpression;
            if (expression == null)
            {
                throw new KbFunctionException($"Parameter {ordinal} could not be converted to an expression.");
            }

            return Helpers.ConvertTo<T?>(expression.Value);
        }

        public FunctionWithParams(string functionName)
        {
            Function = functionName;

            if (AggregateFunctionNames.Contains(Function.ToLower()))
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
