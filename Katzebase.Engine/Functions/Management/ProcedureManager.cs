using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Functions.Parameters;
using Katzebase.Engine.Functions.Procedures;
using Katzebase.Engine.Functions.Procedures.Persistent;
using Katzebase.Engine.Schemas;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using System.Collections.Concurrent;
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

        internal void CreateCustomProcedure(Transaction transaction, string schemaName, string objectName, List<PhysicalProcedureParameter> parameters, List<string> Batches)
        {
            var physicalSchema = core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            var physicalProcedureCatalog = Acquire(transaction, physicalSchema, LockOperation.Write);

            //var batches = KbUtility.SplitQueryBatches(body);

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
                    Batches = Batches,
                };

                physicalProcedureCatalog.Add(physicalProcesure);

                core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), physicalProcedureCatalog);
            }
            else
            {
                physicalProcesure.Parameters = parameters;
                physicalProcesure.Batches = Batches;
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

        internal PhysicalProcedure? Acquire(Transaction transaction, PhysicalSchema physicalSchema, string procedureName, LockOperation intendedOperation)
        {
            procedureName = procedureName.ToLower();

            if (File.Exists(physicalSchema.ProcedureCatalogFilePath()) == false)
            {
                core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), new PhysicalProcedureCatalog());
            }

            var procedureCatalog = core.IO.GetJson<PhysicalProcedureCatalog>(transaction, physicalSchema.ProcedureCatalogFilePath(), intendedOperation);

            return procedureCatalog.Collection.Where(o => o.Name.ToLower() == procedureName).FirstOrDefault();
        }

        internal KbQueryResultCollection ExecuteProcedure(Transaction transaction, FunctionParameterBase procedureCall)
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
                            return new KbQueryResultCollection();
                        }
                    case "releaseallocations":
                        {
                            GC.Collect();
                            return new KbQueryResultCollection();
                        }
                    case "showcacheitems":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();
                            result.AddField("Partition");
                            result.AddField("AproximateSizeInBytes");
                            result.AddField("Created");
                            result.AddField("GetCount");
                            result.AddField("LastGetDate");
                            result.AddField("SetCount");
                            result.AddField("LastSetDate");
                            result.AddField("Key");

                            var cachePartitions = core.Cache.GetPartitionAllocationDetails();

                            foreach (var partition in cachePartitions.Partitions)
                            {
                                var values = new List<string?> {
                                    $"{partition.Partition:n0}",
                                    $"{partition.AproximateSizeInBytes:n0}",
                                    $"{partition.Created}",
                                    $"{partition.GetCount:n0}",
                                    $"{partition.LastGetDate}",
                                    $"{partition.SetCount:n0}",
                                    $"{partition.LastSetDate}",
                                    $"{partition.Key}",
                                };

                                result.AddRow(values);
                            }

                            return collection;
                        }
                    case "showcachepartitions":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("Partition");
                            result.AddField("Allocations");
                            result.AddField("Size (MB)");

                            var cachePartitions = core.Cache.GetPartitionAllocationStatistics();

                            int partitionIndex = 0;

                            foreach (var partition in cachePartitions.Partitions)
                            {
                                var values = new List<string?> {
                                    partitionIndex++.ToString(),
                                    $"{partition.Allocations:n0}",
                                    $"{partition.SizeInKilobytes:n2}"
                                };

                                result.AddRow(values);
                            }

                            return collection;
                        }
                    case "showhealthcounters":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("Counter");
                            result.AddField("Instance");
                            result.AddField("Value");

                            var counters = core.Health.CloneCounters();

                            foreach (var counter in counters)
                            {
                                var values = new List<string?> { counter.Key, counter.Value.Instance, counter.Value.Value.ToString() };

                                result.AddRow(values);
                            }

                            return collection;
                        }
                    case "showwaitinglocks":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("ProcessId");
                            result.AddField("LockType");
                            result.AddField("Operation");
                            result.AddField("ObjectName");

                            var waitingForLocks = core.Locking.Locks.CloneTransactionWaitingForLocks().ToList();

                            var processId = proc.Parameters.GetNullable<ulong?>("processId");
                            if (processId != null)
                            {
                                waitingForLocks = waitingForLocks.Where(o => o.Key.ProcessId == processId).ToList();
                            }

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


                            return collection;
                        }

                    case "showblocks":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("ProcessId");
                            result.AddField("BlockedBy");

                            var transactions = core.Transactions.CloneTransactions();

                            var processId = proc.Parameters.GetNullable<ulong?>("processId");
                            if (processId != null)
                            {
                                transactions = transactions.Where(o => o.ProcessId == processId).ToList();
                            }

                            foreach (var tx in transactions)
                            {
                                var blockedBy = tx.CloneBlocks();

                                foreach (var block in blockedBy)
                                {
                                    var values = new List<string?> { tx.ProcessId.ToString(), block.ToString() };
                                    result.AddRow(values);
                                }
                            }

                            return collection;
                        }
                    case "showtransactions":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("ProcessId");
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
                                tx.ProcessId.ToString(),
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

                            return collection;
                        }
                    case "showprocesses":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

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

                            return collection;
                        }
                    case "clearhealthcounters":
                        {
                            core.Health.ClearCounters();
                            return new KbQueryResultCollection();
                        }
                    case "checkpointhealthcounters":
                        {
                            core.Health.Checkpoint();
                            return new KbQueryResultCollection();
                        }
                }
            }
            else
            {
                //Next check for user procedures in a schema:
                KbUtility.EnsureNotNull(proc.PhysicalSchema);
                KbUtility.EnsureNotNull(proc.PhysicalProcedure);
                KbQueryResultCollection collection = new();

                //We create a "user transaction" so that we have a way to track and destroy temporary objects created by the procedure.
                using (var transactionReference = core.Transactions.Acquire(transaction.ProcessId, true))
                {
                    foreach (var batch in proc.PhysicalProcedure.Batches)
                    {
                        string batchText = batch;

                        foreach (var param in proc.Parameters.Values)
                        {
                            batchText = batchText.Replace(param.Parameter.Name, param.Value, StringComparison.OrdinalIgnoreCase);
                        }

                        var batchStartTime = DateTime.UtcNow;
                        var batchResults = core.Query.APIHandlers.ExecuteStatementQuery(transaction.ProcessId, batchText).Collection.Single();
                        var batchDuration = (DateTime.UtcNow - batchStartTime).TotalMilliseconds;

                        if (batchResults.Success != true)
                        {
                            throw new KbEngineException("Procedure batch was unsuccessful.");
                        }

                        collection.Add(batchResults);
                    }
                    transactionReference.Commit();
                }

                collection.Success = true;

                return collection;
            }

            throw new KbFunctionException($"Undefined procedure [{procedureName}].");
        }
    }
}
