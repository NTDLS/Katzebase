using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Functions.Procedures.Persistent;
using NTDLS.Katzebase.Engine.Query;
using NTDLS.Katzebase.Engine.Sessions;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to procedures.
    /// </summary>
    internal class ProcedureQueryHandlers
    {
        private readonly EngineCore _core;

        public ProcedureQueryHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to instantiate procedures query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteCreate(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);

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
                Management.LogManager.Error($"Failed to execute procedure create for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbQueryResultCollection ExecuteExec(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                var result = _core.Procedures.ExecuteProcedure(transactionReference.Transaction, preparedQuery.ProcedureCall);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, 0);
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute procedure for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
