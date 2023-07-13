using Katzebase.Engine.Functions.Aggregate.Parameters;
using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Functions.Aggregate
{
    internal static class AggregateFunctionCollection
    {
        private static List<AggregateFunction>? _protypes = null;

        public static void Initialize()
        {
            if (_protypes == null)
            {
                _protypes = new List<AggregateFunction>();

                foreach (var prototype in AggregateFunctionImplementation.FunctionPrototypes)
                {
                    _protypes.Add(AggregateFunction.Parse(prototype));
                }
            }
        }

        public static AggregateFunctionParameterValueCollection ApplyFunctionPrototype(string functionName, List<AggregateGenericParameter> parameters)
        {
            if (_protypes == null)
            {
                throw new KbFatalException("Function prototypes were not initialized.");
            }

            var function = _protypes.Where(o => o.Name.ToLower() == functionName.ToLower()).FirstOrDefault();

            if (function == null)
            {
                throw new KbFunctionException($"Undefined function: {functionName}.");
            }

            return function.ApplyParameters(parameters);
        }
    }
}
