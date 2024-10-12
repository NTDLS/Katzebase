using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemSleep
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var timeoutMilliseconds = function.Get<int?>("timeoutMilliseconds");
            if (timeoutMilliseconds != null)
            {
                Thread.Sleep(timeoutMilliseconds.EnsureNotNull());
            }
            return new KbQueryResultCollection();
        }
    }
}
