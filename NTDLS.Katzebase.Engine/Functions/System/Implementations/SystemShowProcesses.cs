using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowProcesses
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
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

            var processId = function.GetNullable<ulong?>("processId");
            if (processId != null)
            {
                sessions = sessions.Where(o => o.Value.ProcessId == processId).ToDictionary(o => o.Key, o => o.Value);
            }

            foreach (var s in sessions)
            {
                var txSnapshot = txSnapshots.FirstOrDefault(o => o.ProcessId == s.Value.ProcessId);

                var values = new List<string?>
                {
                    $"{s.Key}",
                    $"{s.Value.ProcessId:n0}",
                    $"{s.Value.ClientName ?? string.Empty}",
                    $"{s.Value.LoginTime}",
                    $"{s.Value.LastCheckInTime}",
                    $"{txSnapshot?.BlockedByKeys.Count > 0:n0}",
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
