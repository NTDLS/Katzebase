using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Query.Function
{
    internal static class QueryFunctionCollection
    {
        private static List<QueryFunction>? _protypes = null;

        public static void Initialize()
        {
            if (_protypes == null)
            {
                _protypes = new List<QueryFunction>();

                foreach (var prototype in QueryFunctionImplementation.FunctionPrototypes)
                {
                    _protypes.Add(QueryFunction.Parse(prototype));
                }
            }
        }

        public static QueryFunctionParameterValueCollection ApplyMethodPrototype(string methodName, List<string> parameters)
        {
            if (_protypes == null)
            {
                throw new KbFatalException("Method prototypes were not initialized.");
            }

            var method = _protypes.Where(o => o.Name.ToLower() == methodName.ToLower()).FirstOrDefault();

            if (method == null)
            {
                throw new KbMethodException($"Undefined method: {methodName}.");
            }

            return method.ApplyParameters(parameters);
        }
    }
}
