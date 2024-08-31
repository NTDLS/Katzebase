using System.Diagnostics.CodeAnalysis;

namespace ParserV2.StandIn
{
    internal static class ScalerFunctionCollection
    {
        private static List<ScalerFunction>? _prototypes = null;

        public static List<ScalerFunction> Prototypes
        {
            get
            {
                if (_prototypes == null)
                {
                    _prototypes =
                    [
                        new ScalerFunction("Concat", Types.KbScalerFunctionParameterType.String),
                        new ScalerFunction("Length", Types.KbScalerFunctionParameterType.Numeric),
                    ];
                }

                return _prototypes;
            }
        }

        public static bool TryGetFunction(string name, [NotNullWhen(true)] out ScalerFunction? function)
        {
            function = Prototypes.FirstOrDefault(o => o.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return function != null;
        }
    }
}
