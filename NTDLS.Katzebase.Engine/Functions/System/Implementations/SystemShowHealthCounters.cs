using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowHealthCounters
    {
        public static KbQueryResultCollection Execute(EngineCore core, Transaction transaction, SystemFunctionParameterValueCollection function)
        {
            var collection = new KbQueryResultCollection();
            var result = collection.AddNew();

            result.AddField("Counter");
            result.AddField("Value");

            var counters = core.Health.CloneCounters();

            foreach (var counter in counters)
            {
                var values = new List<string?>
                {
                    Text.SeperateCamelCase(counter.Key),
                    counter.Value.Value.ToString("n0")
                };

                result.AddRow(values);
            }

            return collection;
        }
    }
}
