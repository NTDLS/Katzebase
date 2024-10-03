using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Interfaces;
using NTDLS.Katzebase.Parsers.Functions.System;
namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    public static class SystemShowBlocks
    {
        public static KbQueryResultCollection<TData> Execute<TData>(EngineCore<TData> core, Transaction<TData> transaction, SystemFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            var collection = new KbQueryResultCollection<TData>();
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
                    var values = new List<TData> { 
                        txSnapshot.ProcessId.ToString().CastToT<TData> (EngineCore<TData>.StrCast)
                        , block.ToString().CastToT<TData> (EngineCore<TData>.StrCast) 
                    };
                    result.AddRow(values);
                }
            }

            return collection;
        }
    }
}
