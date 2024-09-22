using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowTransactions
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
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

            var processId = function.GetNullable<ulong?>("processId");
            if (processId != null)
            {
                txSnapshots = txSnapshots.Where(o => o.ProcessId == processId).ToList();
            }

            foreach (var txSnapshot in txSnapshots)
            {
                var values = new List<string?>
                {
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
    }
}
