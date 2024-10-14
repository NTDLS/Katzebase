using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowMySchemaPolicy
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var collection = new KbQueryResultCollection();
            var result = collection.AddNew();

            result.AddField("Schema");
            result.AddField("Permission");
            result.AddField("Rule");
            result.AddField("InheritedFromRole");
            result.AddField("InheritedFromSchema");

            var schemaName = function.Get<string>("schemaName").EnsureNotNull();

            var policies = core.Policy.GetCurrentAccountSchemaPermission(transaction, schemaName);

            foreach (var policy in policies)
            {
                var values = new List<string?>
                {
                    schemaName,
                    policy.Value.Permission.ToString(),
                    policy.Value.Rule.ToString(),
                    policy.Value.InheritedFromRole,
                    policy.Value.InheritedFromSchema
                };
                result.AddRow(values);
            }

            return collection;
        }
    }
}
