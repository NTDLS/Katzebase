using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemSleep
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var timeoutMilliseconds = function.Get<int>("timeoutMilliseconds");
            Thread.Sleep(timeoutMilliseconds);
            return  new KbQueryResultCollection();
        }
    }
}
