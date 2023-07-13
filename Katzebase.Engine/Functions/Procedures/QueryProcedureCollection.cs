using Katzebase.Engine.Functions.Parameters;
using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Functions.Procedures
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

        public static QueryProcedureParameterValueCollection ApplyProcedurePrototype(string procedureName, List<FunctionParameterBase> parameters)
        {
            if (_systemProcedureProtypes == null)
            {
                throw new KbFatalException("Procedure prototypes were not initialized.");
            }

            var procedure = _systemProcedureProtypes.Where(o => o.Name.ToLower() == procedureName.ToLower()).FirstOrDefault();

            if (procedure == null)
            {
                throw new KbFunctionException($"Undefined procedure: {procedureName}.");
            }

            return procedure.ApplyParameters(parameters);
        }
    }
}
