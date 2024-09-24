using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using fs;
namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowBlocks
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var collection = new KbQueryResultCollection();
            var result = collection.AddNew();

            result.AddField("Process Id");
            result.AddField("Blocked By");

            var txSnapshots = core.Transactions.Snapshot();

            var processId = function.GetNullable<ulong?>("processId");
            if (processId != null)
            {
                txSnapshots = txSnapshots.Where(o => o.ProcessId == processId).ToList();
            }

            foreach (var txSnapshot in txSnapshots)
            {
                foreach (var block in txSnapshot.BlockedByKeys)
                {
                    var values = new List<fstring?> {
                        fstring.NewS(txSnapshot.ProcessId.ToString()),
                        fstring.NewS(block.ToString()) };
                    result.AddRow(values);
                }
            }

            return collection;
        }
    }
}
