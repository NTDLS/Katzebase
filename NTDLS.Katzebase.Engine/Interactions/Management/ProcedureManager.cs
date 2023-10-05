using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Functions.Parameters;
using NTDLS.Katzebase.Engine.Functions.Procedures;
using NTDLS.Katzebase.Engine.Functions.Procedures.Persistent;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Interactions.APIHandlers;
using NTDLS.Katzebase.Engine.Interactions.QueryHandlers;
using NTDLS.Katzebase.Engine.Schemas;
using System.Diagnostics;
using System.Text;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to procedures.
    /// </summary>
    public class ProcedureManager
    {
        private readonly Core _core;

        internal ProcedureQueryHandlers QueryHandlers { get; private set; }
        public ProcedureAPIHandlers APIHandlers { get; private set; }

        public ProcedureManager(Core core)
        {
            _core = core;

            try
            {
                QueryHandlers = new ProcedureQueryHandlers(core);
                APIHandlers = new ProcedureAPIHandlers(core);

                ProcedureCollection.Initialize();
            }
            catch (Exception ex)
            {
                core.Log.Write("Failed to instantiate procedures manager.", ex);
                throw;
            }
        }

        internal void CreateCustomProcedure(Transaction transaction, string schemaName, string objectName, List<PhysicalProcedureParameter> parameters, List<string> Batches)
        {
            var physicalSchema = _core.Schemas.Acquire(transaction, schemaName, LockOperation.Write);
            var physicalProcedureCatalog = Acquire(transaction, physicalSchema, LockOperation.Write);

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

                _core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), physicalProcedureCatalog);
            }
            else
            {
                physicalProcesure.Parameters = parameters;
                physicalProcesure.Batches = Batches;
                physicalProcesure.Modfied = DateTime.UtcNow;

                _core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), physicalProcedureCatalog);
            }
        }

        internal PhysicalProcedureCatalog Acquire(Transaction transaction, PhysicalSchema physicalSchema, LockOperation intendedOperation)
        {
            if (File.Exists(physicalSchema.ProcedureCatalogFilePath()) == false)
            {
                _core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), new PhysicalProcedureCatalog());
            }

            return _core.IO.GetJson<PhysicalProcedureCatalog>(transaction, physicalSchema.ProcedureCatalogFilePath(), intendedOperation);
        }

        internal PhysicalProcedure? Acquire(Transaction transaction, PhysicalSchema physicalSchema, string procedureName, LockOperation intendedOperation)
        {
            procedureName = procedureName.ToLower();

            if (File.Exists(physicalSchema.ProcedureCatalogFilePath()) == false)
            {
                _core.IO.PutJson(transaction, physicalSchema.ProcedureCatalogFilePath(), new PhysicalProcedureCatalog());
            }

            var procedureCatalog = _core.IO.GetJson<PhysicalProcedureCatalog>(transaction, physicalSchema.ProcedureCatalogFilePath(), intendedOperation);

            return procedureCatalog.Collection.Where(o => o.Name.ToLower() == procedureName).FirstOrDefault();
        }

        internal KbQueryResultCollection ExecuteProcedure(Transaction transaction, FunctionParameterBase procedureCall)
        {
            string procedureName = string.Empty;

            AppliedProcedurePrototype? proc = null;

            if (procedureCall is FunctionConstantParameter)
            {
                var procCall = (FunctionConstantParameter)procedureCall;
                procedureName = procCall.RawValue;
                proc = ProcedureCollection.ApplyProcedurePrototype(_core, transaction, procCall.RawValue, new List<FunctionParameterBase>());
            }
            else if (procedureCall is FunctionWithParams)
            {
                var procCall = (FunctionWithParams)procedureCall;
                procedureName = procCall.Function;
                proc = ProcedureCollection.ApplyProcedurePrototype(_core, transaction, procCall.Function, procCall.Parameters);
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
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "clearcacheallocations":
                        {
                            _core.Cache.Clear();
                            return new KbQueryResultCollection();
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "releasecacheallocations":
                        {
                            GC.Collect();
                            return new KbQueryResultCollection();
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "showmemoryutilization":
                        {
                            var cachePartitions = _core.Cache.GetPartitionAllocationDetails();
                            long totalCacheSize = 0;
                            foreach (var partition in cachePartitions.Partitions)
                            {
                                totalCacheSize += partition.AproximateSizeInBytes;
                            }

                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();
                            result.AddField("WorkingSet64");
                            result.AddField("MinWorkingSet");
                            result.AddField("MaxWorkingSet");
                            result.AddField("PeakWorkingSet64");
                            result.AddField("PagedMemorySize64");
                            result.AddField("NonpagedSystemMemorySize64");
                            result.AddField("PeakVirtualMemorySize64");
                            result.AddField("VirtualMemorySize64");
                            result.AddField("PrivateMemorySize64");
                            result.AddField("TotalCacheSize");

                            var process = Process.GetCurrentProcess();
                            var values = new List<string?> {
                                    $"{process.WorkingSet64:n0}",
                                    $"{process.MinWorkingSet:n0}",
                                    $"{process.MaxWorkingSet:n0}",
                                    $"{process.PeakWorkingSet64:n0}",
                                    $"{process.PagedMemorySize64:n0}",
                                    $"{process.NonpagedSystemMemorySize64:n0}",
                                    $"{process.PeakPagedMemorySize64:n0}",
                                    $"{process.PeakVirtualMemorySize64:n0}",
                                    $"{process.VirtualMemorySize64:n0}",
                                    $"{process.PrivateMemorySize64:n0}",
                                    $"{totalCacheSize:n0}",
                            };

                            result.AddRow(values);

                            return collection;
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "showcacheallocations":
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

                            var cachePartitions = _core.Cache.GetPartitionAllocationDetails();

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
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "showcachepartitions":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("Partition");
                            result.AddField("Allocations");
                            result.AddField("Size (KB)");
                            result.AddField("MaxSize (KB)");

                            var cachePartitions = _core.Cache.GetPartitionAllocationStatistics();

                            foreach (var partition in cachePartitions.Partitions)
                            {
                                var values = new List<string?> {
                                    $"{partition.Partition:n0}",
                                    $"{partition.Allocations:n0}",
                                    $"{partition.SizeInKilobytes:n2}",
                                    $"{partition.MaxSizeInKilobytes:n2}"
                                };

                                result.AddRow(values);
                            }

                            return collection;
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "showhealthcounters":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("Counter");
                            result.AddField("Instance");
                            result.AddField("Value");

                            var counters = _core.Health.CloneCounters();

                            foreach (var counter in counters)
                            {
                                var values = new List<string?> { counter.Key, counter.Value.Instance, counter.Value.Value.ToString() };

                                result.AddRow(values);
                            }

                            return collection;
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "showblocktree":
                        {
                            //TODO Take a snap of transaction block, find the headers, display the a wait trees.

                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            var txSnapshots = _core.Transactions.Snapshot();

                            var allBlocks = txSnapshots.SelectMany(o => o.BlockedByKeys).ToList();

                            var blockHeaders = txSnapshots.Where(tx =>
                                tx.BlockedByKeys.Count == 0 //Transaction is not blocked.
                                && allBlocks.Where(o => o.ProcessId == tx.ProcessId).Any() //Transaction is blocking other transatransactions.
                            ).ToList();

                            var helpText = new StringBuilder();

                            foreach (var blocker in blockHeaders)
                            {
                                RecurseBlocks(txSnapshots, blocker, 0, ref helpText);
                            }

                            void RecurseBlocks(List<TransactionSnapshot> txSnapshots, TransactionSnapshot blockingTx, int level, ref StringBuilder helpText)
                            {
                                var blockedTxs = txSnapshots.Where(o => o.BlockedByKeys.Where(o => o.ProcessId == blockingTx.ProcessId).Any()).ToList();
                                if (blockedTxs.Any() == false)
                                {
                                    return;
                                }

                                helpText.AppendLine(Str(level) + "Blocking Process {");
                                helpText.AppendLine(Str(level + 1) + $"PID: {blockingTx.ProcessId}");
                                helpText.AppendLine(Str(level + 1) + $"Operation: {blockingTx.TopLevelOperation}");
                                helpText.AppendLine(Str(level + 1) + $"StartTime: {blockingTx.StartTime}");
                                if (blockingTx.CurrentLockIntention != null)
                                {
                                    var age = (DateTime.UtcNow - (blockingTx.CurrentLockIntention?.CreationTime ?? DateTime.UtcNow)).TotalMilliseconds;
                                    helpText.AppendLine(Str(level + 1) + $"Intention: {blockingTx.CurrentLockIntention?.ToString()} ({age:n0}ms)");
                                }

                                foreach (var blockedTx in blockedTxs)
                                {
                                    var blockedTxWaitKeys = blockedTx.BlockedByKeys.Where(o => o.ProcessId == blockingTx.ProcessId).ToList();

                                    helpText.AppendLine();
                                    helpText.AppendLine(Str(level + 1) + "Blocked Process {");
                                    helpText.AppendLine(Str(level + 2) + $"PID: {blockedTx.ProcessId}");
                                    helpText.AppendLine(Str(level + 2) + $"Operation: {blockedTx.TopLevelOperation}");
                                    helpText.AppendLine(Str(level + 2) + $"StartTime: {blockedTx.StartTime}");
                                    if (blockedTx.CurrentLockIntention != null)
                                    {
                                        var age = (DateTime.UtcNow - (blockedTx.CurrentLockIntention?.CreationTime ?? DateTime.UtcNow)).TotalMilliseconds;
                                        helpText.AppendLine(Str(level + 2) + $"Intention: {blockedTx.CurrentLockIntention?.ToString()} ({age:n0}ms)");
                                    }
                                    helpText.AppendLine(Str(level + 2) + "Blocking Keys {");
                                    foreach (var key in blockedTxWaitKeys)
                                    {
                                        var age = (DateTime.UtcNow - key.IssueTime).TotalMilliseconds;
                                        helpText.AppendLine(Str(level + 3) + $"{key.ToString()} ({age:n0}ms)");
                                    }
                                    helpText.AppendLine(Str(level + 2) + "}");
                                    helpText.AppendLine(Str(level + 1) + "}");

                                    RecurseBlocks(txSnapshots, blockedTx, level + 1, ref helpText);
                                }
                                helpText.AppendLine(Str(level) + "}");
                                helpText.AppendLine();

                                string Str(int count) => (new string(' ', count * 4));
                            }

                            result.Messages.Add(new KbQueryResultMessage(helpText.ToString(), KbConstants.KbMessageType.Verbose));

                            return collection;
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "showwaitinglocks":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("ProcessId");
                            result.AddField("Granularity");
                            result.AddField("Operation");
                            result.AddField("ObjectName");

                            var waitingTxSnapshots = _core.Locking.Locks.SnapshotWaitingTransactions().ToList();

                            var processId = proc.Parameters.GetNullable<ulong?>("processId");
                            if (processId != null)
                            {
                                waitingTxSnapshots = waitingTxSnapshots.Where(o => o.Key.ProcessId == processId).ToList();
                            }

                            foreach (var waitingForLock in waitingTxSnapshots)
                            {
                                var values = new List<string?> {
                                    waitingForLock.Key.ProcessId.ToString(),
                                    waitingForLock.Value.Granularity.ToString(),
                                    waitingForLock.Value.Operation.ToString(),
                                    waitingForLock.Value.ObjectName.ToString(),
                                };
                                result.AddRow(values);
                            }

                            return collection;
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "showblocks":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("ProcessId");
                            result.AddField("BlockedBy");

                            var txSnapshots = _core.Transactions.Snapshot();

                            var processId = proc.Parameters.GetNullable<ulong?>("processId");
                            if (processId != null)
                            {
                                txSnapshots = txSnapshots.Where(o => o.ProcessId == processId).ToList();
                            }

                            foreach (var txSnapshot in txSnapshots)
                            {
                                foreach (var block in txSnapshot.BlockedByKeys)
                                {
                                    var values = new List<string?> { txSnapshot.ProcessId.ToString(), block.ToString() };
                                    result.AddRow(values);
                                }
                            }

                            return collection;
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "showtransactions":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("ProcessId");
                            result.AddField("IsBlocked");
                            result.AddField("BlockedBy");
                            result.AddField("References");
                            result.AddField("StartTime");
                            result.AddField("HeldLockKeys");
                            result.AddField("GrantedLocks");
                            result.AddField("CachedForRead");
                            result.AddField("DeferredIOs");
                            result.AddField("IsActive");
                            result.AddField("IsDeadlocked");
                            result.AddField("IsCancelled");
                            result.AddField("IsUserCreated");

                            var txSnapshots = _core.Transactions.Snapshot();

                            var processId = proc.Parameters.GetNullable<ulong?>("processId");
                            if (processId != null)
                            {
                                txSnapshots = txSnapshots.Where(o => o.ProcessId == processId).ToList();
                            }

                            foreach (var txSnapshot in txSnapshots)
                            {
                                var values = new List<string?> {
                                    $"{txSnapshot.ProcessId:n0}",
                                    $"{(txSnapshot?.BlockedByKeys.Count > 0):n0}",
                                    string.Join(", ", txSnapshot?.BlockedByKeys.Select(o=>o.ProcessId) ?? new List<ulong>()),
                                    $"{txSnapshot?.ReferenceCount:n0}",
                                    $"{txSnapshot?.StartTime}",
                                    $"{txSnapshot?.HeldLockKeys.Count:n0}",
                                    $"{txSnapshot?.GrantedLockCache?.Count:n0}",
                                    $"{txSnapshot?.FilesReadForCache?.Count:n0}",
                                    $"{txSnapshot?.DeferredIOs?.Count():n0}",
                                    $"{!(txSnapshot?.IsComittedOrRolledBack == true)}",
                                    $"{txSnapshot?.IsDeadlocked}",
                                    $"{txSnapshot?.IsCancelled}",
                                    $"{txSnapshot?.IsUserCreated}"
                                };
                                result.AddRow(values);
                            }

                            return collection;
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "showprocesses":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("SessionId");
                            result.AddField("ProcessId");
                            result.AddField("ClientName");
                            result.AddField("LoginTime");
                            result.AddField("LastCheckinTime");

                            result.AddField("IsBlocked");
                            result.AddField("BlockedBy");
                            result.AddField("References");
                            result.AddField("StartTime");
                            result.AddField("HeldLockKeys");
                            result.AddField("GrantedLocks");
                            result.AddField("CachedForRead");
                            result.AddField("DeferredIOs");
                            result.AddField("IsActive");
                            result.AddField("IsDeadlocked");
                            result.AddField("IsCancelled");
                            result.AddField("IsUserCreated");

                            var sessions = _core.Sessions.CloneSessions();
                            var txSnapshots = _core.Transactions.Snapshot();

                            var processId = proc.Parameters.GetNullable<ulong?>("processId");
                            if (processId != null)
                            {
                                txSnapshots = txSnapshots.Where(o => o.ProcessId == processId).ToList();
                            }

                            foreach (var session in sessions)
                            {
                                var txSnapshot = txSnapshots.Where(o => o.ProcessId == session.Value.ProcessId).FirstOrDefault();

                                var values = new List<string?> {
                                    $"{session.Key}",
                                    $"{session.Value.ProcessId:n0}",
                                    $"{session.Value.ClientName ?? string.Empty}",
                                    $"{session.Value.LoginTime}",
                                    $"{session.Value.LastCheckinTime}",
                                    $"{(txSnapshot?.BlockedByKeys.Count > 0):n0}",
                                    string.Join(", ", txSnapshot?.BlockedByKeys.Select(o=>o.ProcessId) ?? new List<ulong>()),
                                    $"{txSnapshot?.ReferenceCount:n0}",
                                    $"{txSnapshot?.StartTime}",
                                    $"{txSnapshot?.HeldLockKeys.Count:n0}",
                                    $"{txSnapshot?.GrantedLockCache?.Count:n0}",
                                    $"{txSnapshot?.FilesReadForCache?.Count:n0}",
                                    $"{txSnapshot?.DeferredIOs?.Count():n0}",
                                    $"{!(txSnapshot?.IsComittedOrRolledBack == true)}",
                                    $"{txSnapshot?.IsDeadlocked}",
                                    $"{txSnapshot?.IsCancelled}",
                                    $"{txSnapshot?.IsUserCreated}"
                                };
                                result.AddRow(values);
                            }

                            return collection;
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "clearhealthcounters":
                        {
                            _core.Health.ClearCounters();
                            return new KbQueryResultCollection();
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "checkpointhealthcounters":
                        {
                            _core.Health.Checkpoint();
                            return new KbQueryResultCollection();
                        }
                    case "systemscalerfunctions":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("Name");
                            result.AddField("Return Type");
                            result.AddField("Parameters");

                            foreach (var prototype in ScalerFunctionCollection.Prototypes)
                            {
                                var parameters = new StringBuilder();

                                foreach (var param in prototype.Parameters)
                                {
                                    parameters.Append($"{param.Name}:{param.Type}");
                                    if (param.HasDefault)
                                    {
                                        parameters.Append($" = {param.DefaultValue}");
                                    }
                                    parameters.Append(", ");
                                }
                                if (parameters.Length > 2)
                                {
                                    parameters.Length -= 2;
                                }

                                var values = new List<string?> {
                                    prototype.Name,
                                    prototype.ReturnType.ToString(),
                                    parameters.ToString()
                                };
                                result.AddRow(values);

#if DEBUG
                                //This is to provide code for the documentation wiki.
                                var wikiProtitype = new StringBuilder();

                                wikiProtitype.Append($"##Color(#318000, {prototype.ReturnType})");
                                wikiProtitype.Append($" ##Color(#c6680e, {prototype.Name})(");

                                if (prototype.Parameters.Count > 0)
                                {
                                    for (int i = 0; i < prototype.Parameters.Count; i++)
                                    {
                                        var param = prototype.Parameters[i];

                                        wikiProtitype.Append($"##Color(#318000, {param.Type}) ##Color(#c6680e, {param.Name})");
                                        if (param.HasDefault)
                                        {
                                            wikiProtitype.Append($" = ##Color(#CC0000, \"'{param.DefaultValue}'\")");
                                        }
                                        wikiProtitype.Append(", ");
                                    }
                                    if (wikiProtitype.Length > 2)
                                    {
                                        wikiProtitype.Length -= 2;
                                    }
                                }
                                wikiProtitype.Append($")");
                                result.Messages.Add(new KbQueryResultMessage(wikiProtitype.ToString(), KbConstants.KbMessageType.Verbose));
#endif

                            }

                            return collection;
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
                using (var transactionReference = _core.Transactions.Acquire(transaction.ProcessId, true))
                {
                    foreach (var batch in proc.PhysicalProcedure.Batches)
                    {
                        string batchText = batch;

                        foreach (var param in proc.Parameters.Values)
                        {
                            batchText = batchText.Replace(param.Parameter.Name, param.Value, StringComparison.OrdinalIgnoreCase);
                        }

                        var batchStartTime = DateTime.UtcNow;
                        var batchResults = _core.Query.APIHandlers.ExecuteStatementQuery(transaction.ProcessId, batchText).Collection.Single();
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
