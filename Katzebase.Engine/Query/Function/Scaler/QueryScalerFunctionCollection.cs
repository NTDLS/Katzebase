using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Query.Function.Scaler
{
    internal static class QueryScalerFunctionCollection
    {
        private static List<QueryScalerFunction>? _protypes = null;

        public static void Initialize()
        {
            if (_protypes == null)
            {
                _protypes = new List<QueryScalerFunction>();

                foreach (var prototype in QueryScalerFunctionImplementation.FunctionPrototypes)
                {
                    _protypes.Add(QueryScalerFunction.Parse(prototype));
                }
            }
        }

        public static QueryScalerFunctionParameterValueCollection ApplyFunctionPrototype(string functionName, List<string?> parameters)
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
