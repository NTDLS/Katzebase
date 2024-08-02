using NTDLS.Helpers;
using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Functions.Parameters;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Engine.Query;
using System.Diagnostics;
using System.Text;
using static System.Formats.Asn1.AsnWriter;

namespace NTDLS.Katzebase.Engine.Functions.Procedures
{

    internal class SystemProcedureImplementations
    {
        internal static KbQueryResultCollection ExecuteProcedure(EngineCore core, Transaction transaction, FunctionParameterBase procedureCall)
        {
            string procedureName = string.Empty;

            AppliedProcedurePrototype? proc = null;

            if (procedureCall is FunctionConstantParameter functionConstantParameter)
            {
                procedureName = functionConstantParameter.RawValue;
                proc = ProcedureCollection.ApplyProcedurePrototype(core, transaction, functionConstantParameter.RawValue, new());
            }
            else if (procedureCall is FunctionWithParams functionWithParams)
            {
                procedureName = functionWithParams.Function;
                proc = ProcedureCollection.ApplyProcedurePrototype(core, transaction, functionWithParams.Function, functionWithParams.Parameters);
            }
            else
            {
                throw new KbNotImplementedException("Procedure call type is not implemented");
            }

            if (proc.IsSystem)
            {
                //First check for system procedures:
                switch (procedureName.ToLowerInvariant())
                {
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "clearcacheallocations":
                        {
                            core.Cache.Clear();
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
                            var cachePartitions = core.Cache.GetPartitionAllocationDetails();
                            long totalCacheSize = 0;
                            foreach (var partition in cachePartitions.Items)
                            {
                                totalCacheSize += partition.ApproximateSizeInBytes;
                            }

                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();
                            result.AddField("Working Set");
                            result.AddField("Min. Working Set");
                            result.AddField("Max. WorkingSet");
                            result.AddField("Peak Working Set");
                            result.AddField("Paged Memory");
                            result.AddField("Non-paged System Memory");
                            result.AddField("Peak Paged Memory");
                            result.AddField("Peak Virtual Memory");
                            result.AddField("Virtual Memory");
                            result.AddField("Private Memory");
                            result.AddField("Cache Size");

                            var process = Process.GetCurrentProcess();

                            var values = new List<string?> {
                                    $"{Formatters.FileSize(process.WorkingSet64)}",
                                    $"{Formatters.FileSize(process.MinWorkingSet)}",
                                    $"{Formatters.FileSize(process.MaxWorkingSet)}",
                                    $"{Formatters.FileSize(process.PeakWorkingSet64)}",
                                    $"{Formatters.FileSize(process.PagedMemorySize64)}",
                                    $"{Formatters.FileSize(process.NonpagedSystemMemorySize64)}",
                                    $"{Formatters.FileSize(process.PeakPagedMemorySize64)}",
                                    $"{Formatters.FileSize(process.PeakVirtualMemorySize64)}",
                                    $"{Formatters.FileSize(process.VirtualMemorySize64)}",
                                    $"{Formatters.FileSize(process.PrivateMemorySize64)}",
                                    $"{Formatters.FileSize(totalCacheSize)}",
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
                            result.AddField("Approximate Size");
                            result.AddField("Created");
                            result.AddField("Reads");
                            result.AddField("Last Read");
                            result.AddField("Writes");
                            result.AddField("Last Write");
                            result.AddField("Key");

                            var cachePartitions = core.Cache.GetPartitionAllocationDetails();

                            foreach (var item in cachePartitions.Items)
                            {
                                var values = new List<string?> {
                                    $"{item.Partition:n0}",
                                    $"{Formatters.FileSize(item.ApproximateSizeInBytes)}",
                                    $"{item.Created}",
                                    $"{item.Reads:n0}",
                                    $"{item.LastRead}",
                                    $"{item.Writes:n0}",
                                    $"{item.LastWrite}",
                                    $"{item.Key}",
                                };

                                result.AddRow(values);
                            }

                            return collection;
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "showcachepages":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();
                            result.AddField("Partition");
                            result.AddField("Approximate Size");
                            result.AddField("Created");
                            result.AddField("Reads");
                            result.AddField("Last Read");
                            result.AddField("Writes");
                            result.AddField("Last Write");
                            result.AddField("Documents");
                            result.AddField("Key");

                            var cachePartitions = core.Cache.GetPartitionAllocationDetails();

                            foreach (var item in cachePartitions.Items.Where(o => o.Key.EndsWith(EngineConstants.DocumentPageExtension)))
                            {
                                if (core.Cache.TryGet(item.Key, out var pageObject))
                                {
                                    if (pageObject is PhysicalDocumentPage page)
                                    {
                                        var values = new List<string?> {
                                            $"{item.Partition:n0}",
                                            $"{Formatters.FileSize(item.ApproximateSizeInBytes)}",
                                            $"{item.Created}",
                                            $"{item.Reads:n0}",
                                            $"{item.LastRead}",
                                            $"{item.Writes:n0}",
                                            $"{item.LastWrite}",
                                            $"{page.Documents.Count:n0}",
                                            $"{item.Key}"
                                        };
                                        result.AddRow(values);
                                    }
                                }
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
                            result.AddField("Size");
                            result.AddField("Max Size");

                            var cachePartitions = core.Cache.GetPartitionAllocationStatistics();

                            foreach (var partition in cachePartitions.Partitions)
                            {
                                var values = new List<string?> {
                                    $"{partition.Partition:n0}",
                                    $"{partition.Count:n0}",
                                    $"{Formatters.FileSize(partition.SizeInBytes)}",
                                    $"{Formatters.FileSize(partition.Configuration.MaxMemoryBytes):n2}"
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

                            var counters = core.Health.CloneCounters();

                            foreach (var counter in counters)
                            {
                                var values = new List<string?>
                                {
                                    Text.SeperateCamelCase(counter.Key),
                                    Text.SeperateCamelCase(counter.Value.Instance),
                                    counter.Value.Value.ToString()
                                };

                                result.AddRow(values);
                            }

                            return collection;
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "showblocktree":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            var sessions = core.Sessions.CloneSessions();
                            var txSnapshots = core.Transactions.Snapshot();

                            var allBlocks = txSnapshots.SelectMany(o => o.BlockedByKeys).ToList();

                            var blockHeaders = txSnapshots.Where(tx =>
                                tx.BlockedByKeys.Count == 0 //Transaction is not blocked.
                                && allBlocks.Any(o => o.ProcessId == tx.ProcessId) //Transaction is blocking other transactions.
                            ).ToList();

                            var helpText = new StringBuilder();

                            foreach (var blocker in blockHeaders)
                            {
                                RecurseBlocks(txSnapshots, blocker, 0, ref helpText);
                            }

                            void RecurseBlocks(List<TransactionSnapshot> txSnapshots, TransactionSnapshot blockingTx, int level, ref StringBuilder helpText)
                            {
                                var blockingSession = sessions.Where(o => o.Value.ProcessId == blockingTx.ProcessId).Select(o => o.Value).First();

                                var blockedTxs = txSnapshots.Where(o => o.BlockedByKeys.Where(o => o.ProcessId == blockingTx.ProcessId).Any()).ToList();
                                if (blockedTxs.Count == 0)
                                {
                                    return;
                                }

                                helpText.AppendLine(Str(level) + "Blocking Process {");
                                helpText.AppendLine(Str(level + 1) + $"PID: {blockingTx.ProcessId}");
                                helpText.AppendLine(Str(level + 1) + $"Client: {blockingSession.ClientName}");
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
                                    var blockedSession = sessions.Where(o => o.Value.ProcessId == blockingTx.ProcessId).Select(o => o.Value).First();

                                    helpText.AppendLine();
                                    helpText.AppendLine(Str(level + 1) + "Blocked Process {");
                                    helpText.AppendLine(Str(level + 2) + $"PID: {blockedTx.ProcessId}");
                                    helpText.AppendLine(Str(level + 2) + $"Client: {blockedSession.ClientName}");
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
                    case "showlocks":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("ProcessId");
                            result.AddField("Granularity");
                            result.AddField("Operation");
                            result.AddField("Object Name");

                            var txSnapshots = core.Transactions.Snapshot();

                            var processId = proc.Parameters.GetNullable<ulong?>("processId");
                            if (processId != null)
                            {
                                txSnapshots = txSnapshots.Where(o => o.ProcessId == processId).ToList();
                            }

                            foreach (var tx in txSnapshots)
                            {
                                foreach (var heldLockKey in tx.HeldLockKeys)
                                {

                                    var values = new List<string?> {
                                        heldLockKey.ProcessId.ToString(),
                                        heldLockKey.ObjectLock.Granularity.ToString(),
                                        heldLockKey.Operation.ToString(),
                                        heldLockKey.ObjectName.ToString(),
                                    };
                                    result.AddRow(values);
                                }
                            }

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
                            result.AddField("Object Name");

                            var waitingTxSnapshots = core.Locking.Locks.SnapshotWaitingTransactions().ToList();

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
                    case "terminate":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            var processId = proc.Parameters.Get<ulong>("processId");

                            core.Sessions.CloseByProcessId(processId);

                            return  new KbQueryResultCollection();
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "showblocks":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("Process Id");
                            result.AddField("Blocked By");

                            var txSnapshots = core.Transactions.Snapshot();

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

                            result.AddField("Process Id");
                            result.AddField("Blocked?");
                            result.AddField("Blocked By");
                            result.AddField("References");
                            result.AddField("Start Time");
                            result.AddField("Held Lock Keys");
                            result.AddField("Granted Locks");
                            result.AddField("Cached for Read");
                            result.AddField("Deferred IOs");
                            result.AddField("Active?");
                            result.AddField("Deadlocked?");
                            result.AddField("Cancelled?");
                            result.AddField("User Created?");

                            var txSnapshots = core.Transactions.Snapshot();

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
                                    $"{!(txSnapshot?.IsCommittedOrRolledBack == true)}",
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

                            result.AddField("Session Id");
                            result.AddField("Process Id");
                            result.AddField("Client Name");
                            result.AddField("Login Time");
                            result.AddField("Last Check-in");

                            result.AddField("Blocked?");
                            result.AddField("Blocked By");
                            result.AddField("References");
                            result.AddField("Start Time");
                            result.AddField("Held Lock Keys");
                            result.AddField("Granted Locks");
                            result.AddField("Cached for Read");
                            result.AddField("Deferred IOs");
                            result.AddField("Active?");
                            result.AddField("Deadlocked?");
                            result.AddField("Cancelled?");
                            result.AddField("User Created?");

                            var sessions = core.Sessions.CloneSessions();
                            var txSnapshots = core.Transactions.Snapshot();

                            var processId = proc.Parameters.GetNullable<ulong?>("processId");
                            if (processId != null)
                            {
                                txSnapshots = txSnapshots.Where(o => o.ProcessId == processId).ToList();
                            }

                            foreach (var s in sessions)
                            {
                                var txSnapshot = txSnapshots.FirstOrDefault(o => o.ProcessId == s.Value.ProcessId);

                                var values = new List<string?> {
                                    $"{s.Key}",
                                    $"{s.Value.ProcessId:n0}",
                                    $"{s.Value.ClientName ?? string.Empty}",
                                    $"{s.Value.LoginTime}",
                                    $"{s.Value.LastCheckInTime}",
                                    $"{(txSnapshot?.BlockedByKeys.Count > 0):n0}",
                                    string.Join(", ", txSnapshot?.BlockedByKeys.Select(o=>o.ProcessId) ?? new List<ulong>()),
                                    $"{txSnapshot?.ReferenceCount:n0}",
                                    $"{txSnapshot?.StartTime}",
                                    $"{txSnapshot?.HeldLockKeys.Count:n0}",
                                    $"{txSnapshot?.GrantedLockCache?.Count:n0}",
                                    $"{txSnapshot?.FilesReadForCache?.Count:n0}",
                                    $"{txSnapshot?.DeferredIOs?.Count():n0}",
                                    $"{!(txSnapshot?.IsCommittedOrRolledBack == true)}",
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
                            core.Health.ClearCounters();
                            return new KbQueryResultCollection();
                        }
                    //---------------------------------------------------------------------------------------------------------------------------
                    case "checkpointhealthcounters":
                        {
                            core.Health.Checkpoint();
                            return new KbQueryResultCollection();
                        }
                    case "showsystemscalerfunctions":
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
                                var wikiPrototype = new StringBuilder();

                                wikiPrototype.Append($"##Color(#318000, {prototype.ReturnType})");
                                wikiPrototype.Append($" ##Color(#c6680e, {prototype.Name})(");

                                if (prototype.Parameters.Count > 0)
                                {
                                    for (int i = 0; i < prototype.Parameters.Count; i++)
                                    {
                                        var param = prototype.Parameters[i];

                                        wikiPrototype.Append($"##Color(#318000, {param.Type}) ##Color(#c6680e, {param.Name})");
                                        if (param.HasDefault)
                                        {
                                            wikiPrototype.Append($" = ##Color(#CC0000, \"'{param.DefaultValue}'\")");
                                        }
                                        wikiPrototype.Append(", ");
                                    }
                                    if (wikiPrototype.Length > 2)
                                    {
                                        wikiPrototype.Length -= 2;
                                    }
                                }
                                wikiPrototype.Append(')');
                                result.Messages.Add(new KbQueryResultMessage(wikiPrototype.ToString(), KbConstants.KbMessageType.Verbose));
#endif
                            }

                            return collection;
                        }
                    case "showsystemprocedures":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("Name");
                            result.AddField("Parameters");

                            foreach (var prototype in ProcedureCollection.Prototypes)
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
                                    parameters.ToString()
                                };
                                result.AddRow(values);

#if DEBUG
                                //This is to provide code for the documentation wiki.
                                var wikiPrototype = new StringBuilder();

                                wikiPrototype.Append($" ##Color(#c6680e, {prototype.Name})(");

                                if (prototype.Parameters.Count > 0)
                                {
                                    for (int i = 0; i < prototype.Parameters.Count; i++)
                                    {
                                        var param = prototype.Parameters[i];

                                        wikiPrototype.Append($"##Color(#318000, {param.Type}) ##Color(#c6680e, {param.Name})");
                                        if (param.HasDefault)
                                        {
                                            wikiPrototype.Append($" = ##Color(#CC0000, \"'{param.DefaultValue}'\")");
                                        }
                                        wikiPrototype.Append(", ");
                                    }
                                    if (wikiPrototype.Length > 2)
                                    {
                                        wikiPrototype.Length -= 2;
                                    }
                                }
                                wikiPrototype.Append(')');
                                result.Messages.Add(new KbQueryResultMessage(wikiPrototype.ToString(), KbConstants.KbMessageType.Verbose));
#endif
                            }

                            return collection;
                        }

                    case "showsystemaggregatefunctions":
                        {
                            var collection = new KbQueryResultCollection();
                            var result = collection.AddNew();

                            result.AddField("Name");
                            result.AddField("Parameters");

                            foreach (var prototype in AggregateFunctionCollection.Prototypes)
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
                                    parameters.ToString()
                                };
                                result.AddRow(values);

#if DEBUG
                                //This is to provide code for the documentation wiki.
                                var wikiPrototype = new StringBuilder();

                                wikiPrototype.Append($" ##Color(#c6680e, {prototype.Name})(");

                                if (prototype.Parameters.Count > 0)
                                {
                                    for (int i = 0; i < prototype.Parameters.Count; i++)
                                    {
                                        var param = prototype.Parameters[i];

                                        wikiPrototype.Append($"##Color(#318000, {param.Type}) ##Color(#c6680e, {param.Name})");
                                        if (param.HasDefault)
                                        {
                                            wikiPrototype.Append($" = ##Color(#CC0000, \"'{param.DefaultValue}'\")");
                                        }
                                        wikiPrototype.Append(", ");
                                    }
                                    if (wikiPrototype.Length > 2)
                                    {
                                        wikiPrototype.Length -= 2;
                                    }
                                }
                                wikiPrototype.Append(')');
                                result.Messages.Add(new KbQueryResultMessage(wikiPrototype.ToString(), KbConstants.KbMessageType.Verbose));
#endif
                            }

                            return collection;
                        }

                }
            }
            else
            {
                //Next check for user procedures in a schema:
                proc.PhysicalSchema.EnsureNotNull();
                proc.PhysicalProcedure.EnsureNotNull();
                KbQueryResultCollection collection = new();

                var session = core.Sessions.ByProcessId(transaction.ProcessId);

                //We create a "user transaction" so that we have a way to track and destroy temporary objects created by the procedure.
                using (var transactionReference = core.Transactions.Acquire(session, true))
                {
                    foreach (var batch in proc.PhysicalProcedure.Batches)
                    {
                        string batchText = batch;

                        foreach (var param in proc.Parameters.Values)
                        {
                            batchText = batchText.Replace(param.Parameter.Name, param.Value, StringComparison.OrdinalIgnoreCase);
                        }

                        var batchStartTime = DateTime.UtcNow;

                        var batchResults = new KbQueryResultCollection();
                        foreach (var preparedQuery in StaticQueryParser.PrepareBatch(batchText))
                        {
                            batchResults.Add(core.Query.ExecuteQuery(session, preparedQuery));
                        }

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
