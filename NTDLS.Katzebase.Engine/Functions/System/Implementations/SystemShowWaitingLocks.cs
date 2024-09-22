using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowWaitingLocks
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var collection = new KbQueryResultCollection();
            var result = collection.AddNew();

            result.AddField("ProcessId");
            result.AddField("Granularity");
            result.AddField("Operation");
            result.AddField("Object Name");

            var waitingTxSnapshots = core.Locking.SnapshotWaitingTransactions().ToList();

            var processId = function.GetNullable<ulong?>("processId");
            if (processId != null)
            {
                waitingTxSnapshots = waitingTxSnapshots.Where(o => o.Key.ProcessId == processId).ToList();
            }

            foreach (var waitingForLock in waitingTxSnapshots)
            {
                var values = new List<string?>
                {
                    waitingForLock.Key.ProcessId.ToString(),
                    waitingForLock.Value.Granularity.ToString(),
                    waitingForLock.Value.Operation.ToString(),
                    waitingForLock.Value.ObjectName.ToString(),
                };
                result.AddRow(values);
            }

            return collection;
        }
    }
}
