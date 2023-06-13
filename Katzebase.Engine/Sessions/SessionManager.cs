using Katzebase.Engine.Query;
using Katzebase.Engine.Trace;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.Trace.PerformanceTrace;

namespace Katzebase.Engine.Sessions
{
    /// <summary>
    /// This is the class that all API controllers should interface with for session access.
    /// </summary>
    public class SessionManager
    {
        private Core core;
        private ulong nextProcessId = 1;
        internal Dictionary<Guid, SessionState> Collection { get; set; } = new();

        public SessionManager(Core core)
        {
            this.core = core;
        }

        public ulong UpsertSessionId(Guid sessionId)
        {
            lock (Collection)
            {
                if (Collection.ContainsKey(sessionId))
                {
                    return Collection[sessionId].ProcessId;
                }
                else
                {
                    ulong processId = nextProcessId++;
                    Collection.Add(sessionId, new SessionState(processId, sessionId));
                    return processId;
                }
            }
        }

        public SessionState ByProcessId(ulong sessionId)
        {
            lock (Collection)
            {
                var result = Collection.Where(o => o.Value.ProcessId == sessionId).FirstOrDefault();
                if (result.Value != null)
                {
                    return result.Value;
                }
                throw new KbSessionNotFoundException($"The session was not found: {sessionId}");
            }
        }

        internal KbActionResponse ExecuteSetVariable(ulong processId, PreparedQuery preparedQuery)
        {
            try
            {
                var result = new KbActionResponse();
                var pt = new PerformanceTrace();

                var ptAcquireTransaction = pt?.BeginTrace(PerformanceTraceType.AcquireTransaction);
                using (var transaction = core.Transactions.Begin(processId))
                {
                    ptAcquireTransaction?.EndTrace();

                    var session = ByProcessId(processId);

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
                            string systemVariableName = variable.Name.ToLower();
                            if (systemVariableName == "tracewaittimes")
                            {
                                session.TraceWaitTimesEnabled = VariableOnOff(variable.Value.ToLower());
                            }
                            else
                            {
                                throw new KbGenericException($"Unknown system variable: {variable.Name}.");
                            }
                        }
                    }

                    transaction.Commit();
                }

                return result;
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to ExecuteSetVariable for process {processId}.", ex);
                throw;
            }
        }
    }
}
