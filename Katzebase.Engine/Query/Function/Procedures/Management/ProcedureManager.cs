using Katzebase.Engine.Query.FunctionParameter;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;

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

        internal KbQueryResult ExecuteProcedure(FunctionParameterBase procedureCall)
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
                        return new KbQueryResult();
                    }
                case "releasefreeallocations":
                    {
                        GC.Collect();
                        return new KbQueryResult();
                    }
                case "getallocationpartitions":
                    {
                        var result = new KbQueryResult();

                        result.AddField("Partition");
                        result.AddField("Allocations");

                        var partitionAllocations = core.Cache.GetAllocations();

                        int partition = 0;

                        foreach (var partitionAllocation in partitionAllocations.PartitionAllocations)
                        {
                            var values = new List<string?> { (partition++).ToString(), partitionAllocation.ToString() };

                            result.AddRow(values);
                        }

                        return result;
                    }
            }

            //TODO: Next check for user procedures in a schema:
            //...

            throw new KbMethodException($"Unknown procedure [{methodName}].");
        }
    }
}
