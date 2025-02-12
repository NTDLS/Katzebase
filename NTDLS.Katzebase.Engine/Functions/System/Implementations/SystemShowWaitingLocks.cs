﻿using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;

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
            result.AddField("Wait Time (ms)");
            result.AddField("Object Name");

            var waitingTxSnapshots = core.Locking.SnapshotWaitingTransactions().ToList();

            var processId = function.Get<ulong?>("processId");
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
                    Convert.ToInt64((DateTime.UtcNow - waitingForLock.Value.CreationTime).TotalMilliseconds).ToString(),
                    waitingForLock.Value.ObjectName.ToString()
                };
                result.AddRow(values);
            }

            return collection;
        }
    }
}
