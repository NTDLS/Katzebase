using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Query;
using static NTDLS.Katzebase.Engine.Sessions.SessionState;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to sessions.
    /// </summary>
    internal class SessionQueryHandlers
    {
        private readonly Core _core;

        public SessionQueryHandlers(Core core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instantiate session query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteKillProcess(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var referencedProcessId = preparedQuery.Attribute<ulong>(PreparedQuery.QueryAttribute.ProcessId);

                _core.Sessions.CloseByProcessId(referencedProcessId);
                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to execute variable set for process id {processId}.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteSetVariable(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var session = _core.Sessions.ByProcessId(processId);

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
                _core.Log.Write($"Failed to execute variable set for process id {processId}.", ex);
                throw;
            }
        }
    }
}
