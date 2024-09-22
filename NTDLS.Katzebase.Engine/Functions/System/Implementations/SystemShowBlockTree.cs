using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using System.Text;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowBlockTree
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
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
    }
}
