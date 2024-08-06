using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Query;
using NTDLS.Katzebase.Engine.Sessions;
using static NTDLS.Katzebase.Engine.Sessions.SessionState;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to sessions.
    /// </summary>
    internal class SessionQueryHandlers
    {
        private readonly EngineCore _core;

        public SessionQueryHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to instantiate session query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteKillProcess(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                var referencedProcessId = preparedQuery.Attribute<ulong>(PreparedQuery.QueryAttribute.ProcessId);

                _core.Sessions.CloseByProcessId(referencedProcessId);
                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute variable set for process id {session.ProcessId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteSetVariable(SessionState session, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);

                foreach (var variable in preparedQuery.VariableValues)
                {
                    if (Enum.TryParse(variable.Name, true, out KbConnectionSetting connectionSetting) == false
                        || Enum.IsDefined(typeof(KbConnectionSetting), connectionSetting) == false)
                    {
                        throw new KbGenericException($"Unknown system variable: {variable.Name}.");
                    }

                    switch (connectionSetting)
                    {
                        case KbConnectionSetting.TraceWaitTimes:
                            session.UpsertConnectionSetting(connectionSetting, bool.Parse(variable.Value) ? 1 : 0);
                            break;
                        case KbConnectionSetting.ExplainQuery:
                            session.UpsertConnectionSetting(connectionSetting, bool.Parse(variable.Value) ? 1 : 0);
                            break;
                        default:
                            throw new KbNotImplementedException();
                    }
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                Management.LogManager.Error($"Failed to execute variable set for process id {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
