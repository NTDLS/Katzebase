using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Functions.Aggregate.Parameters;
using NTDLS.Katzebase.Shared;
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
    }
}
