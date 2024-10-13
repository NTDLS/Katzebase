using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Functions.System;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowPolicy
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var collection = new KbQueryResultCollection();
            var result = collection.AddNew();

            result.AddField("Schema");
            result.AddField("Permission");
            result.AddField("Rule");

            var schemaName = function.Get<string>("schemaName").EnsureNotNull();

            var policies = core.Policy.GetCurrentAccountSchemaPermission(transaction, schemaName);

            foreach (var policy in policies)
            {

                var values = new List<string?>
                {
                    schemaName,
                    policy.Key.ToString(),
                    policy.Value.ToString(),
                };
                result.AddRow(values);
            }

            return collection;
        }
    }
}
