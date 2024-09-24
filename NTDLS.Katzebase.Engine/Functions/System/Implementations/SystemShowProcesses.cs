using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using fs;
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

                var values = new List<fstring?>
                {
                    fstring.NewS($"{s.Key}"),
                    fstring.NewS($"{s.Value.ProcessId:n0}"),
                    fstring.NewS($"{s.Value.ClientName ?? string.Empty}"),
                    fstring.NewS($"{s.Value.LoginTime}"),
                    fstring.NewS($"{s.Value.LastCheckInTime}"),
                    fstring.NewS($"{txSnapshot?.BlockedByKeys.Count > 0:n0}"),
                    fstring.NewS(string.Join(", ", txSnapshot?.BlockedByKeys.Select(o=>o.ProcessId) ?? new List<ulong>())),
                    fstring.NewS($"{txSnapshot?.ReferenceCount:n0}"),
                    fstring.NewS($"{txSnapshot?.StartTime}"),
                    fstring.NewS($"{txSnapshot?.HeldLockKeys.Count:n0}"),
                    fstring.NewS($"{txSnapshot?.GrantedLockCache?.Count:n0}"),
                    fstring.NewS($"{txSnapshot?.FilesReadForCache?.Count:n0}"),
                    fstring.NewS($"{txSnapshot?.DeferredIOs?.Count():n0}"),
                    fstring.NewS($"{!(txSnapshot?.IsCommittedOrRolledBack == true)}"),
                    fstring.NewS($"{txSnapshot?.IsDeadlocked}"),
                    fstring.NewS($"{txSnapshot?.IsCancelled}"),
                    fstring.NewS($"{txSnapshot?.IsUserCreated}")
                };
                result.AddRow(values);
            }

            return collection;
        }
    }
}
