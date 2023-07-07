using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Query.Function.Aggregate
{
    internal static class QueryAggregateFunctionCollection
    {
        private static List<QueryAggregateFunction>? _protypes = null;

        public static void Initialize()
        {
            if (_protypes == null)
            {
                _protypes = new List<QueryAggregateFunction>();

                foreach (var prototype in QueryAggregateFunctionImplementation.FunctionPrototypes)
                {
                    _protypes.Add(QueryAggregateFunction.Parse(prototype));
                }
            }
        }

        public static QueryAggregateFunctionParameterValueCollection ApplyFunctionPrototype(string functionName, List<string?> parameters)
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
