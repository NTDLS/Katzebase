using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemRefreshMyRoles
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var results = new KbQueryResultCollection();
            var result = results.AddNew();

            transaction.Session.Roles = transaction.ExecuteQuery<KbRole>("AccountRoles.kbs",
                new
                {
                    Username = transaction.Session.Username
                }).ToList();

            foreach (var role in transaction.Session.Roles)
            {
                var message = $"{role.Name}" + (role.IsAdministrator ? " (Administrator)" : "");
                result.Messages.Add(new KbQueryResultMessage(message, Api.KbConstants.KbMessageType.User));
            }

            return results;
        }
    }
}
