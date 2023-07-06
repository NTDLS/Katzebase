using Katzebase.Engine.Query.FunctionParameter;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;

namespace Katzebase.Engine.Query.Function.Procedures.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to functions.
    /// </summary>
    public class ProcedureManager
    {
        private readonly Core core;
        internal ProcedureQueryHandlers QueryHandlers { get; set; }
        public ProcedureAPIHandlers APIHandlers { get; set; }

        public ProcedureManager(Core core)
        {
            this.core = core;

            try
            {
                QueryHandlers = new ProcedureQueryHandlers(core);
                APIHandlers = new ProcedureAPIHandlers(core);
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate functions manager.", ex);
                throw;
            }
        }

        internal KbQueryResult ExecuteProcedure(FunctionParameterBase procedureCall)
        {
            string methodName = string.Empty;

            if (procedureCall is FunctionConstantParameter)
            {
                methodName = ((FunctionConstantParameter)procedureCall).Value;
            }
            else if (procedureCall is FunctionMethodAndParams)
            {
                methodName = ((FunctionMethodAndParams)procedureCall).Method;
            }

            //First check for system procedures:
            switch (methodName.ToLower())
            {
                case "clearcache":
                    {
                        core.Cache.Clear();
                        return new KbQueryResult();
                    }
                case "releasecacheallocations":
                    {
                        GC.Collect();
                        return new KbQueryResult();
                    }
                case "showcachepartitions":
                    {
                        var result = new KbQueryResult();

                        result.AddField("Partition");
                        result.AddField("Allocations");

                        var partitionAllocations = core.Cache.GetAllocations();

                        int partition = 0;

                        foreach (var partitionAllocation in partitionAllocations.PartitionAllocations)
                        {
                            var values = new List<string?> { (partition++).ToString(), partitionAllocation.ToString() };

                            result.AddRow(values);
                        }

                        return result;
                    }
                case "showhealthcounters":
                    {
                        var result = new KbQueryResult();

                        result.AddField("Counter");
                        result.AddField("Instance");
                        result.AddField("Value");

                        var counters = core.Health.CloneCounters();

                        foreach (var counter in counters)
                        {
                            var values = new List<string?> { counter.Key, counter.Value.Instance, counter.Value.Value.ToString() };

                            result.AddRow(values);
                        }

                        return result;
                    }
                case "showwaitinglocks":
                    {
                        //var procCall = (FunctionMethodAndParams)procedureCall;
                        //var processId = procCall.GetParam<ulong>(0);
                        var waitingForLocks = core.Locking.Locks.CloneTransactionWaitingForLocks();

                        var result = new KbQueryResult();

                        result.AddField("ProcessId");
                        result.AddField("LockType");
                        result.AddField("Operation");
                        result.AddField("ObjectName");


                        foreach (var waitingForLock in waitingForLocks)
                        {
                            var values = new List<string?> {
                                    waitingForLock.Key.ProcessId.ToString(),
                                    waitingForLock.Value.LockType.ToString(),
                                    waitingForLock.Value.Operation.ToString(),
                                    waitingForLock.Value.ObjectName.ToString(),
                                };
                            result.AddRow(values);
                        }


                        return result;
                    }

                case "showblocks":
                    {
                        var procCall = (FunctionMethodAndParams)procedureCall;
                        var processId = procCall.GetParam<ulong>(0);
                        var transaction = core.Transactions.CloneTransactions().Where(o => o.ProcessId == processId).FirstOrDefault();

                        var result = new KbQueryResult();

                        result.AddField("ProcessId");

                        if (transaction != null)
                        {
                            var blockedBy = transaction.CloneBlocks();

                            foreach (var block in blockedBy)
                            {
                                var values = new List<string?> {
                                    block.ToString()
                                };
                                result.AddRow(values);
                            }
                        }

                        return result;
                    }
                case "showtransactions":
                    {
                        var result = new KbQueryResult();

                        result.AddField("TxBlocked");
                        result.AddField("TxReferences");
                        result.AddField("TxStartTime");
                        result.AddField("TxHeldLockKeys");
                        result.AddField("TxGrantedLocks");
                        result.AddField("TxDeferredIOs");
                        result.AddField("TxActive");
                        result.AddField("TxDeadlocked");
                        result.AddField("TxCancelled");
                        result.AddField("TxUserCreated");

                        var transactions = core.Transactions.CloneTransactions();

                        foreach (var transaction in transactions)
                        {

                            var values = new List<string?> {
                                (transaction?.BlockedBy.Count > 0).ToString(),
                                transaction?.ReferenceCount.ToString(),
                                transaction?.StartTime.ToString(),
                                transaction?.HeldLockKeys?.Count.ToString(),
                                transaction?.GrantedLockCache?.Count.ToString(),
                                transaction?.DeferredIOs?.Count().ToString(),
                                (!(transaction?.IsComittedOrRolledBack == true)).ToString(),
                                transaction?.IsDeadlocked.ToString(),
                                transaction?.IsCancelled.ToString(),
                                transaction?.IsUserCreated.ToString()
                            };
                            result.AddRow(values);
                        }

                        return result;
                    }
                case "showprocesses":
                    {
                        var result = new KbQueryResult();

                        result.AddField("SessionId");
                        result.AddField("ProcessId");
                        result.AddField("LoginTime");
                        result.AddField("LastCheckinTime");
                        result.AddField("TxBlocked");
                        result.AddField("TxReferences");
                        result.AddField("TxStartTime");
                        result.AddField("TxHeldLockKeys");
                        result.AddField("TxGrantedLocks");
                        result.AddField("TxDeferredIOs");
                        result.AddField("TxActive");
                        result.AddField("TxDeadlocked");
                        result.AddField("TxCancelled");
                        result.AddField("TxUserCreated");

                        var sessions = core.Sessions.CloneSessions();
                        var transactions = core.Transactions.CloneTransactions();

                        foreach (var session in sessions)
                        {
                            var transaction = transactions.Where(o => o.ProcessId == session.Value.ProcessId).FirstOrDefault();

                            var values = new List<string?> {
                                session.Key.ToString(),
                                session.Value.ProcessId.ToString(),
                                session.Value.LoginTime.ToString(),
                                session.Value.LastCheckinTime.ToString(),
                                (transaction?.BlockedBy.Count > 0).ToString(),
                                transaction?.ReferenceCount.ToString(),
                                transaction?.StartTime.ToString(),
                                transaction?.HeldLockKeys?.Count.ToString(),
                                transaction?.GrantedLockCache?.Count.ToString(),
                                transaction?.DeferredIOs?.Count().ToString(),
                                (!(transaction?.IsComittedOrRolledBack == true)).ToString(),
                                transaction?.IsDeadlocked.ToString(),
                                transaction?.IsCancelled.ToString(),
                                transaction?.IsUserCreated.ToString()
                            };
                            result.AddRow(values);
                        }

                        return result;
                    }
                case "clearhealthcounters":
                    {
                        core.Health.ClearCounters();
                        return new KbQueryResult();
                    }
                case "checkpointhealthcounters":
                    {
                        core.Health.Checkpoint();
                        return new KbQueryResult();
                    }
            }

            //TODO: Next check for user procedures in a schema:
            //...

            throw new KbFunctionException($"Unknown procedure [{methodName}].");
        }
    }
}
