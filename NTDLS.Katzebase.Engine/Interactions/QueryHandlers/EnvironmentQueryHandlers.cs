using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using static NTDLS.Katzebase.Parsers.Constants;


using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to configuration.
    /// </summary>
    internal class EnvironmentQueryHandlers<TData> where TData : IStringable
    {
        private readonly EngineCore<TData> _core;

        public EnvironmentQueryHandlers(EngineCore<TData> core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to instantiate environment query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteAlter(SessionState session, PreparedQuery<TData> preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);

                if (preparedQuery.SubQueryType == SubQueryType.Configuration)
                {
                    _core.Environment.Alter(transactionReference.Transaction, preparedQuery.Attributes);
                }
                else
                {
                    throw new KbNotImplementedException();
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute environment alter for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
