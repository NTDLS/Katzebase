using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.Katzebase.Parsers;
using System.Diagnostics;
using static NTDLS.Katzebase.Shared.EngineConstants;

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

        internal KbActionResponse ExecuteKillProcess(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                #region Security policy enforcment.

                _core.Policy.EnforceAdministratorPolicy(transactionReference.Transaction);

                #endregion

                var referencedProcessId = query.GetAttribute<ulong>(PreparedQuery.Attribute.ProcessId);

                _core.Sessions.TryCloseByProcessID(referencedProcessId);
                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        internal KbActionResponse ExecuteSetVariable(SessionState session, PreparedQuery query)
        {
            try
            {
                using var transactionReference = _core.Transactions.APIAcquire(session);

                foreach (var variable in query.VariableValues)
                {
                    if (Enum.TryParse(variable.Name, true, out StateSetting setting) == false
                        || Enum.IsDefined(typeof(StateSetting), setting) == false)
                    {
                        throw new KbProcessingException($"Unknown session setting: [{variable.Name}].");
                    }

                    switch (setting)
                    {
                        case StateSetting.TraceWaitTimes:
                            session.UpsertConnectionSetting(setting, Helpers.Converters.ConvertTo<bool>(variable.Value));
                            break;
                        case StateSetting.WarnMissingFields:
                            session.UpsertConnectionSetting(setting, Helpers.Converters.ConvertTo<bool>(variable.Value));
                            break;
                        case StateSetting.WarnNullPropagation:
                            session.UpsertConnectionSetting(setting, Helpers.Converters.ConvertTo<bool>(variable.Value));
                            break;
                        case StateSetting.ReadUncommitted:
                            session.UpsertConnectionSetting(setting, Helpers.Converters.ConvertTo<bool>(variable.Value));
                            break;
                        default:
                            throw new KbNotImplementedException($"Unhandled session setting: [{variable.Name}].");
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
