using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemPrint
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var results = new KbQueryResultCollection();
            var result = results.AddNew();

            var expression = function.Get<string>("expression");
            if (expression != null)
            {
                result.Messages.Add(new KbQueryResultMessage(expression, Api.KbConstants.KbMessageType.User));
            }

            //transaction.AddMessage(expression, Api.KbConstants.KbMessageType.Deadlock);

            return results;
        }
    }
}
