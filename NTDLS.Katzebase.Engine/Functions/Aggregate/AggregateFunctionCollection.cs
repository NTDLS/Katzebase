using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Engine.Functions.Aggregate
{
    public static class AggregateFunctionCollection
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

        public static bool TryGetFunction(string name, [NotNullWhen(true)] out AggregateFunction? function)
        {
            function = Prototypes.FirstOrDefault(o => o.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return function != null;
        }

        public static AggregateFunctionParameterValueCollection ApplyFunctionPrototype(string functionName, List<string?> parameters)
        {
            if (_protypes == null)
            {
                throw new KbFatalException("Function prototypes were not initialized.");
            }

            var function = _protypes.FirstOrDefault(o => o.Name.Is(functionName));

            if (function == null)
            {
                throw new KbFunctionException($"Undefined function: [{functionName}].");
            }

            return function.ApplyParameters(parameters);
        }
    }
}
