using Katzebase.Engine.Query.FunctionParameter;

namespace Katzebase.Engine.Query.Function.Procedures.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to functions.
    /// </summary>
    public class ProcedureManager
    {
        private readonly Core core;
        internal ProcedureQueryHandlers QueryHandlers { get; set; }
        public ProcedureAPIHandlers APIHandlers { get; set; }

        public ProcedureManager(Core core)
        {
            this.core = core;

            try
            {
                QueryHandlers = new ProcedureQueryHandlers(core);
                APIHandlers = new ProcedureAPIHandlers(core);
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate functions manager.", ex);
                throw;
            }
        }

        internal void ExecuteProcedure(FunctionParameterBase procedureCall)
        {
            string methodName = string.Empty;

            if (procedureCall is FunctionConstantParameter)
            {
                methodName = ((FunctionConstantParameter)procedureCall).Value;
            }
            else if (procedureCall is FunctionMethodAndParams)
            {
                methodName = ((FunctionMethodAndParams)procedureCall).Method;
            }

            //First check for system procedures:
            switch (methodName.ToLower())
            {
                case "clearcache":
                    {
                        core.Cache.Clear();
                        return;
                    }
                case "releasefreeallocations":
                    {
                        GC.Collect();
                        return;
                    }
            }

            //TODO: Next check for user procedures in a schema:
            //...
        }
    }
}
