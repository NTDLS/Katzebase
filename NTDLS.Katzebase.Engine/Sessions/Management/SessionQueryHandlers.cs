using Katzebase.Engine.Query;
using Katzebase.Exceptions;
using Katzebase.Payloads;
using static Katzebase.Engine.Sessions.SessionState;

namespace Katzebase.Engine.Sessions.Management
{
    /// <summary>
    /// Internal class methods for handling query requests related to sessions.
    /// </summary>
    internal class SessionQueryHandlers
    {
        private readonly Core core;

        public SessionQueryHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate session query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteKillProcess(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                var referencedProcessId = preparedQuery.Attribute<ulong>(PreparedQuery.QueryAttribute.ProcessId);

                core.Sessions.CloseByProcessId(referencedProcessId);
                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute variable set for process id {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteSetVariable(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                var session = core.Sessions.ByProcessId(processId);

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
                            session.UpsertConnectionSetting(connectionSetting, Boolean.Parse(variable.Value) ? 1 : 0);
                            break;
                        case KbConnectionSetting.MinQueryThreads:
                        case KbConnectionSetting.MaxQueryThreads:
                        case KbConnectionSetting.QueryThreadWeight:
                            session.UpsertConnectionSetting(connectionSetting, double.Parse(variable.Value));
                            break;

                        default:
                            throw new KbNotImplementedException();
                    }

                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute variable set for process id {processId}.", ex);
                throw;
            }
        }
    }
}
