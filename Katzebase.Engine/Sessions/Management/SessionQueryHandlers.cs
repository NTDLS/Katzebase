﻿using Katzebase.Engine.Query;
using Katzebase.Engine.Trace;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.Sessions.SessionState;
using static Katzebase.Engine.Trace.PerformanceTrace;

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

        internal KbActionResponse ExecuteSetVariable(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();

                    var session = core.Sessions.ByProcessId(processId);

                    bool VariableOnOff(string value)
                    {
                        if (value == "on")
                        {
                            return true;
                        }
                        else if (value == "off")
                        {
                            return false;
                        }
                        throw new KbGenericException($"Undefined variable value: {value}.");
                    }

                    foreach (var variable in preparedQuery.VariableValues)
                    {
                        if (variable.Name.StartsWith("@")) //User session variable
                        {
                            session.UpsertVariable(variable.Name.Substring(1), variable.Value);
                        }
                        else //System variable:
                        {
                            if (Enum.TryParse(variable.Name, true, out KbSystemVariable systemVariable) == false
                                || Enum.IsDefined(typeof(KbSystemVariable), systemVariable) == false)
                            {
                                throw new KbGenericException($"Unknown system variable: {variable.Name}.");
                            }

                            switch (systemVariable)
                            {
                                case KbSystemVariable.TraceWaitTimes:
                                    session.TraceWaitTimesEnabled = VariableOnOff(variable.Value.ToLower());
                                    break;
                                case KbSystemVariable.MinQueryThreads:
                                    session.MinQueryThreads = int.Parse(variable.Value);
                                    break;
                                case KbSystemVariable.MaxQueryThreads:
                                    session.MaxQueryThreads = int.Parse(variable.Value);
                                    break;
                                case KbSystemVariable.QueryThreadWeight:
                                    session.QueryThreadWeight = int.Parse(variable.Value);
                                    break;

                                default:
                                    throw new KbNotImplementedException();
                            }
                        }
                    }

                    transaction.Commit();
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to execute variable set for process id {processId}.", ex);
                throw;
            }
        }
    }
}