using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemTerminate
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var processId = function.Get<ulong?>("processId");

            if (processId != null)
            {
                core.Sessions.TryCloseByProcessID(processId.EnsureNotNull());
            }

            return new KbQueryResultCollection();
        }
    }
}
