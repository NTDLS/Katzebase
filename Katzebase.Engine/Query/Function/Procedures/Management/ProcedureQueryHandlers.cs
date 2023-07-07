using Katzebase.PublicLibrary.Payloads;

namespace Katzebase.Engine.Query.Function.Procedures.Management
{
    /// <summary>
    /// Internal class methods for handling query requests related to functions.
    /// </summary>
    internal class ProcedureQueryHandlers
    {
        private readonly Core core;

        public ProcedureQueryHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate functions query handler.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteExec(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = core.Functions.ExecuteProcedure(preparedQuery.ProcedureCall);

                    transaction.Commit();
                    result.RowCount = result.Rows.Count;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute document sample for process id {processId}.", ex);
                throw;
            }
        }
    }
}
