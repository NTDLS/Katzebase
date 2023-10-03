using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Functions.Procedures.Persistent;
using NTDLS.Katzebase.Engine.Query;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to procedures.
    /// </summary>
    internal class ProcedureQueryHandlers
    {
        private readonly Core _core;

        public ProcedureQueryHandlers(Core core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instantiate procedures query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreate(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);

                if (preparedQuery.SubQueryType == SubQueryType.Procedure)
                {
                    var objectName = preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.ObjectName);
                    var objectSchema = preparedQuery.Attribute<string>(PreparedQuery.QueryAttribute.Schema);
                    var parameters = preparedQuery.Attribute<List<PhysicalProcedureParameter>>(PreparedQuery.QueryAttribute.Parameters);
                    var Batches = preparedQuery.Attribute<List<string>>(PreparedQuery.QueryAttribute.Batches);

                    _core.Procedures.CreateCustomProcedure(transactionReference.Transaction, objectSchema, objectName, parameters, Batches);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute procedure create for process id {processId}.", ex);
                throw;
            }
        }

        internal KbQueryResultCollection ExecuteExec(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var result = _core.Procedures.ExecuteProcedure(transactionReference.Transaction, preparedQuery.ProcedureCall);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, 0);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute procedure for process id {processId}.", ex);
                throw;
            }
        }
    }
}
