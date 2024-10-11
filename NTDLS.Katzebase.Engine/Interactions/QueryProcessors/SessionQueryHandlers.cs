using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using System.Diagnostics;
using static NTDLS.Katzebase.Engine.Sessions.SessionState;

namespace NTDLS.Katzebase.Engine.Interactions.QueryProcessors
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
                LogManager.Error($"Failed to instantiate session query handler.", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteKillProcess(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);
                var referencedProcessId = query.GetAttribute<ulong>(Query.Attribute.ProcessId);

                _core.Sessions.CloseByProcessId(referencedProcessId);
                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteSetVariable(SessionState session, Query query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                foreach (var variable in query.VariableValues)
                {
                    if (Enum.TryParse(variable.Name, true, out KbConnectionSetting connectionSetting) == false
                        || Enum.IsDefined(typeof(KbConnectionSetting), connectionSetting) == false)
                    {
                        throw new KbGenericException($"Unknown system variable: [{variable.Name}].");
                    }

                    switch (connectionSetting)
                    {
                        case KbConnectionSetting.TraceWaitTimes:
                            session.UpsertConnectionSetting(connectionSetting, Helpers.Converters.ConvertTo<bool>(variable.Value) ? 1 : 0);
                            break;
                        default:
                            throw new KbNotImplementedException();
                    }
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
