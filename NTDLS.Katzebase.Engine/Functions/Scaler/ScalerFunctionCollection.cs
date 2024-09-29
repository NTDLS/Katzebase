using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Engine.Functions.Scaler
{
    public static class ScalerFunctionCollection
    {
        private static List<ScalerFunction>? _protypes = null;
        public static List<ScalerFunction> Prototypes
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
                _protypes = new();

                foreach (var prototype in ScalerFunctionImplementation.PrototypeStrings)
                {
                    _protypes.Add(ScalerFunction.Parse(prototype));
                }
            }
        }

        public static bool TryGetFunction(string name, [NotNullWhen(true)] out ScalerFunction? function)
        {
            function = Prototypes.FirstOrDefault(o => o.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return function != null;
        }

        public static ScalerFunctionParameterValueCollection ApplyFunctionPrototype(string functionName, List<string?> parameters)
        {
            if (_protypes == null)
            {
                throw new KbFatalException("Function prototypes were not initialized.");
            }

            var function = _protypes.FirstOrDefault(o => o.Name.Is(functionName))
                ?? throw new KbFunctionException($"Undefined scaler function: [{functionName}].");

            return function.ApplyParameters(parameters);
        }
    }
}
