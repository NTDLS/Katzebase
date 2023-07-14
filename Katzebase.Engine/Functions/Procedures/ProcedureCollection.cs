using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Functions.Parameters;
using Katzebase.PublicLibrary.Exceptions;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Functions.Procedures
{
    internal static class ProcedureCollection
    {
        private static List<Procedure>? _systemProcedureProtypes = null;

        public static void Initialize()
        {
            if (_systemProcedureProtypes == null)
            {
                _systemProcedureProtypes = new List<Procedure>();

                foreach (var prototype in SystemProcedureImplementation.SystemProcedurePrototypes)
                {
                    _systemProcedureProtypes.Add(Procedure.Parse(prototype));
                }
            }
        }


        public static AppliedProcedurePrototype ApplyProcedurePrototype(Core core, Transaction transaction, string procedureName, List<FunctionParameterBase> parameters)
        {
            if (_systemProcedureProtypes == null)
            {
                throw new KbFatalException("Procedure prototypes were not initialized.");
            }

            var systemProcedure = _systemProcedureProtypes.Where(o => o.Name.ToLower() == procedureName.ToLower()).FirstOrDefault();
            if (systemProcedure != null)
            {
                return new AppliedProcedurePrototype()
                {
                    IsSystem = true,
                    Name = procedureName,
                    Parameters = systemProcedure.ApplyParameters(parameters)
                };
            }

            int paramStartIndex = procedureName.IndexOf('(');
            paramStartIndex = paramStartIndex < 0 ? procedureName.Length : paramStartIndex;

            string schemaName = string.Empty;

            int endOfSchemaIndex = procedureName.Substring(0, paramStartIndex).LastIndexOf(':');
            if (endOfSchemaIndex > 0)
            {
                schemaName = procedureName.Substring(0, endOfSchemaIndex);
                procedureName = procedureName.Substring(endOfSchemaIndex + 1);
            }

            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Read);

            var physicalProcedure = core.Procedures.Acquire(transaction, physicalSchema, LockOperation.Read, procedureName);
            if (physicalProcedure == null)
            {
                throw new KbFunctionException($"Undefined procedure: {procedureName}.");
            }

            return new AppliedProcedurePrototype()
            {
                IsSystem = false,
                Name = procedureName,
                PhysicalSchema = physicalSchema,
                PhysicalProcedure = physicalProcedure,
                Parameters = physicalProcedure.ApplyParameters(parameters)
            };
        }
    }
}
