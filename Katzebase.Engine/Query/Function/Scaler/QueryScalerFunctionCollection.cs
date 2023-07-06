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

        public static QueryScalerFunctionParameterValueCollection ApplyMethodPrototype(string methodName, List<string?> parameters)
        {
            if (_protypes == null)
            {
                throw new KbFatalException("Method prototypes were not initialized.");
            }

            var method = _protypes.Where(o => o.Name.ToLower() == methodName.ToLower()).FirstOrDefault();

            if (method == null)
            {
                throw new KbFunctionException($"Undefined method: {methodName}.");
            }

            return method.ApplyParameters(parameters);
        }
    }
}
