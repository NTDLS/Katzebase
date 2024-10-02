using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;
using static System.Formats.Asn1.AsnWriter;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemCancel
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var collection = new KbQueryResultCollection();
            var result = collection.AddNew();

            var processId = function.Get<ulong>("processId");

            core.Transactions.CloseByProcessID(processId);

            return new KbQueryResultCollection();
        }
    }
}
