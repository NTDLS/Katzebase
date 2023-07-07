using Katzebase.Engine.Query.FunctionParameter;
using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Query.Function.Procedures
{
    internal static class QueryProcedureCollection
    {
        private static List<QueryProcedure>? _systemProcedureProtypes = null;

        public static void Initialize()
        {
            if (_systemProcedureProtypes == null)
            {
                _systemProcedureProtypes = new List<QueryProcedure>();

                foreach (var prototype in QueryProcedureImplementation.SystemProcedurePrototypes)
                {
                    _systemProcedureProtypes.Add(QueryProcedure.Parse(prototype));
                }
            }
        }

        public static QueryProcedureParameterValueCollection ApplyProcedurePrototype(string functionName, List<FunctionParameterBase> parameters)
        {
            if (_systemProcedureProtypes == null)
            {
                throw new KbFatalException("Function prototypes were not initialized.");
            }

            var function = _systemProcedureProtypes.Where(o => o.Name.ToLower() == functionName.ToLower()).FirstOrDefault();

            if (function == null)
            {
                throw new KbFunctionException($"Undefined function: {functionName}.");
            }

            return function.ApplyParameters(parameters);
        }
    }
}
