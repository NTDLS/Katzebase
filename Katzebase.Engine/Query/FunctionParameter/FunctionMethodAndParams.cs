using Katzebase.Engine.Library;
using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Query.FunctionParameter
{
    internal class FunctionMethodAndParams : FunctionParameterBase
    {
        public string Method { get; set; } = string.Empty;
        public List<FunctionParameterBase> Parameters { get; set; } = new();

        public T GetParam<T>(int ordinal)
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

            return Helpers.ConvertTo<T>(expression.Value);
        }
    }
}
