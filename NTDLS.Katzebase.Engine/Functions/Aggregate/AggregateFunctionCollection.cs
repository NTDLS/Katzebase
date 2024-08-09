using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Functions.Aggregate.Parameters;
using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    internal static class AggregateFunctionCollection
    {
        private static List<AggregateFunction>? _protypes = null;

        public static List<AggregateFunction> Prototypes
        {
            get
            {
                if (_protypes == null)
                {
                    throw new KbFatalException("Function prototypes were not initialized.");
                }
                return _protypes;
            }
        }

        public static void Initialize()
        {
            if (_protypes == null)
            {
                _protypes = new List<AggregateFunction>();

                foreach (var prototype in AggregateFunctionImplementation.PrototypeStrings)
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

            var function = _protypes.FirstOrDefault(o => o.Name.Is(functionName));

            if (function == null)
            {
                throw new KbFunctionException($"Undefined function: {functionName}.");
            }

            return function.ApplyParameters(parameters);
        }
    }
}
