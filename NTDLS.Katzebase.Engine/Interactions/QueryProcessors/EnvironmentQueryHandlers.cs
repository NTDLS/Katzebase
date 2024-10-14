using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using System.Diagnostics;

namespace NTDLS.Katzebase.Engine.Interactions.QueryProcessors
{
    /// <summary>
    /// Internal class methods for handling query requests related to configuration.
    /// </summary>
    internal class EnvironmentQueryHandlers
    {
        private readonly EngineCore _core;

        public EnvironmentQueryHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate environment query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteAlter(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region EnforceSchemaPolicy.

                _core.Policy.EnforceAdministratorPolicy(transactionReference.Transaction);

                #endregion

                var rowCount = _core.Environment.Alter(transactionReference.Transaction, query.Attributes);
                return transactionReference.CommitAndApplyMetricsNonQuery(rowCount);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
