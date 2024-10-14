using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.Parsers.Functions.System;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemRefreshMyRoles
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var results = new KbQueryResultCollection();
            var result = results.AddNew();

            using var systemSession = core.Sessions.CreateEphemeralSystemSession();

            transaction.Session.Roles = core.Query.ExecuteQuery<KbRole>(systemSession.Session, EmbeddedScripts.Load("AccountRoles.kbs"),
                new
                {
                    Username = transaction.Session.Username
                }).ToList();

            foreach (var role in transaction.Session.Roles)
            {
                var message = $"{role.Name}" + (role.IsAdministrator ? " (Administrator)" : "");
                result.Messages.Add(new KbQueryResultMessage(message, Api.KbConstants.KbMessageType.User));
            }


            systemSession.Commit();

            return results;
        }
    }
}
