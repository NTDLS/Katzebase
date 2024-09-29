using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowLocks<TData> where TData : IStringable
    {
        public static KbQueryResultCollection<TData> Execute(EngineCore<TData> core, Transaction<TData> transaction, SystemFunctionParameterValueCollection<TData> function)
        {
            var collection = new KbQueryResultCollection<TData>();
            var result = collection.AddNew();

            result.AddField("ProcessId");
            result.AddField("Granularity");
            result.AddField("Operation");
            result.AddField("Object Name");

            var txSnapshots = core.Transactions.Snapshot();

            var processId = function.GetNullable<ulong?>("processId");
            if (processId != null)
            {
                txSnapshots = txSnapshots.Where(o => o.ProcessId == processId).ToList();
            }

            foreach (var tx in txSnapshots)
            {
                foreach (var heldLockKey in tx.HeldLockKeys)
                {

                    var values = new List<TData>(
                    new[]{
                        heldLockKey.ProcessId.ToString(),
                        heldLockKey.ObjectLock.Granularity.ToString(),
                        heldLockKey.Operation.ToString(),
                        heldLockKey.ObjectName.ToString(),
                    }.Select(s=>s.CastToT<TData>(EngineCore<TData>.StrCast)));
                    result.AddRow(values);
                }
            }

            return collection;
        }
    }
}
