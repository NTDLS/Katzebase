using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Functions.Parameters;
using Katzebase.Engine.Functions.Procedures;
using Katzebase.Engine.Functions.Procedures.Persistent;
using Katzebase.Engine.Schemas;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Functions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to procedures.
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

                ProcedureCollection.Initialize();
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instanciate procedures manager.", ex);
                throw;
            }
        }

        internal void CreateCustomProcedure(Transaction transaction, string schemaName, string objectName, List<PhysicalProcedureParameter> parameters, string body)
        {
            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            var physicalProcedureCatalog = Acquire(transaction, physicalSchema, LockOperation.Write);

            var batches = KbUtility.SplitQueryBatches(body);

            var physicalProcesure = physicalProcedureCatalog.GetByName(objectName);
            if (physicalProcesure == null)
            {
                physicalProcesure = new PhysicalProcedure()
                {
                    Id = Guid.NewGuid(),
                    Name = objectName,
                    Created = DateTime.UtcNow,
                    Modfied = DateTime.UtcNow,
                    Parameters = parameters,
                    Batches = batches,
                };

                physicalProcedureCatalog.Add(physicalProcesure);

                core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), physicalProcedureCatalog);
            }
            else
            {
                physicalProcesure.Parameters = parameters;
                physicalProcesure.Batches = batches;
                physicalProcesure.Modfied = DateTime.UtcNow;

                core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), physicalProcedureCatalog);
            }
        }

        internal PhysicalProcedureCatalog Acquire(Transaction transaction, PhysicalSchema physicalSchema, LockOperation intendedOperation)
        {
            if (File.Exists(physicalSchema.ProcedureCatalogFilePath()) == false)
            {
                core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), new PhysicalProcedureCatalog());
            }

            return core.IO.GetJson<PhysicalProcedureCatalog>(transaction, physicalSchema.ProcedureCatalogFilePath(), intendedOperation);
        }

        internal PhysicalProcedure? Acquire(Transaction transaction, PhysicalSchema physicalSchema, LockOperation intendedOperation, string procedureName)
        {
            procedureName = procedureName.ToLower();

            if (File.Exists(physicalSchema.ProcedureCatalogFilePath()) == false)
            {
                core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), new PhysicalProcedureCatalog());
            }

            var procedureCatalog = core.IO.GetJson<PhysicalProcedureCatalog>(transaction, physicalSchema.ProcedureCatalogFilePath(), intendedOperation);

            return procedureCatalog.Collection.Where(o => o.Name.ToLower() == procedureName).FirstOrDefault();
        }

        internal KbQueryResult ExecuteProcedure(Transaction transaction, FunctionParameterBase procedureCall)
        {
            string procedureName = string.Empty;

            AppliedProcedurePrototype? proc = null;

            if (procedureCall is FunctionConstantParameter)
            {
                var procCall = (FunctionConstantParameter)procedureCall;
                procedureName = procCall.Value;
                proc = ProcedureCollection.ApplyProcedurePrototype(core, transaction, procCall.Value, new List<FunctionParameterBase>());
            }
            else if (procedureCall is FunctionWithParams)
            {
                var procCall = (FunctionWithParams)procedureCall;
                procedureName = procCall.Function;
                proc = ProcedureCollection.ApplyProcedurePrototype(core, transaction, procCall.Function, procCall.Parameters);
            }
            else
            {
                throw new KbNotImplementedException("Procedure call type is not implemented");
            }

            if (proc.IsSystem)
            {
                //First check for system procedures:
                switch (procedureName.ToLower())
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
                                var values = new List<string?> { partition++.ToString(), partitionAllocation.ToString() };

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
                            var waitingForLocks = core.Locking.Locks.CloneTransactionWaitingForLocks().ToList();

                            var processId = proc.Parameters.GetNullable<ulong?>("processId");
                            if (processId != null)
                            {
                                waitingForLocks = waitingForLocks.Where(o => o.Key.ProcessId == processId).ToList();
                            }

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
                            var procCall = (FunctionWithParams)procedureCall;

                            var transactions = core.Transactions.CloneTransactions();

                            var processId = proc.Parameters.GetNullable<ulong?>("processId");
                            if (processId != null)
                            {
                                transactions = transactions.Where(o => o.ProcessId == processId).ToList();
                            }

                            var result = new KbQueryResult();

                            result.AddField("ProcessId");

                            foreach (var tx in transactions)
                            {
                                var blockedBy = tx.CloneBlocks();

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

                            var processId = proc.Parameters.GetNullable<ulong?>("processId");
                            if (processId != null)
                            {
                                transactions = transactions.Where(o => o.ProcessId == processId).ToList();
                            }

                            foreach (var tx in transactions)
                            {
                                var values = new List<string?> {
                                (tx?.BlockedBy.Count > 0).ToString(),
                                tx?.ReferenceCount.ToString(),
                                tx?.StartTime.ToString(),
                                tx?.HeldLockKeys?.Count.ToString(),
                                tx?.GrantedLockCache?.Count.ToString(),
                                tx?.DeferredIOs?.Count().ToString(),
                                (!(tx?.IsComittedOrRolledBack == true)).ToString(),
                                tx?.IsDeadlocked.ToString(),
                                tx?.IsCancelled.ToString(),
                                tx?.IsUserCreated.ToString()
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

                            var processId = proc.Parameters.GetNullable<ulong?>("processId");
                            if (processId != null)
                            {
                                transactions = transactions.Where(o => o.ProcessId == processId).ToList();
                            }

                            foreach (var session in sessions)
                            {
                                var tx = transactions.Where(o => o.ProcessId == session.Value.ProcessId).FirstOrDefault();

                                var values = new List<string?> {
                                session.Key.ToString(),
                                session.Value.ProcessId.ToString(),
                                session.Value.LoginTime.ToString(),
                                session.Value.LastCheckinTime.ToString(),
                                (tx?.BlockedBy.Count > 0).ToString(),
                                tx?.ReferenceCount.ToString(),
                                tx?.StartTime.ToString(),
                                tx?.HeldLockKeys?.Count.ToString(),
                                tx?.GrantedLockCache?.Count.ToString(),
                                tx?.DeferredIOs?.Count().ToString(),
                                (!(tx?.IsComittedOrRolledBack == true)).ToString(),
                                tx?.IsDeadlocked.ToString(),
                                tx?.IsCancelled.ToString(),
                                tx?.IsUserCreated.ToString()
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
            }
            else
            {
                //Next check for user procedures in a schema:
                KbUtility.EnsureNotNull(proc.PhysicalSchema);
                KbUtility.EnsureNotNull(proc.PhysicalProcedure);
                KbQueryResult resultWithRows = new();

                var messages = new List<KbQueryResultMessage>();

                int batchNumber = 0;

                //We create a "user transaction" so that we have a way to track and destroy temporary objects created by the procedure.
                using (var txRef = core.Transactions.Acquire(transaction.ProcessId, true))
                {
                    foreach (var batch in proc.PhysicalProcedure.Batches)
                    {
                        string batchText = batch;

                        foreach (var param in proc.Parameters.Values)
                        {
                            batchText = batchText.Replace(param.Parameter.Name, param.Value, StringComparison.OrdinalIgnoreCase);
                        }

                        var batchStartTime = DateTime.UtcNow;
                        var batchResults = core.Query.APIHandlers.ExecuteStatementQuery(transaction.ProcessId, batchText);
                        var batchDuration = (DateTime.UtcNow - batchStartTime).TotalMilliseconds;

                        if (batchResults.Success != true)
                        {
                            throw new KbEngineException("Procedure batch was unsuccessful.");
                        }

                        messages.AddRange(batchResults.Messages);

                        messages.Add(new KbQueryResultMessage($"Procedure batch {batchNumber + 1} of {proc.PhysicalProcedure.Batches.Count} completed in {batchDuration:n0}ms.  ({batchResults.RowCount} rows affected)", KbConstants.KbMessageType.Verbose));

                        if (batchResults.Rows != null && batchResults.Rows.Count > 0)
                        {
                            resultWithRows = batchResults;
                        }
                        batchNumber++;
                    }
                    txRef.Commit();
                }

                resultWithRows.Messages = messages;

                return resultWithRows;
            }

            throw new KbFunctionException($"Undefined procedure [{procedureName}].");
        }
    }
}
