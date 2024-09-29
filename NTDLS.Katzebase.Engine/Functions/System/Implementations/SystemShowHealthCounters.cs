using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemShowHealthCounters 
    {
        public static KbQueryResultCollection<TData> Execute<TData>(EngineCore<TData> core, Transaction<TData> transaction, SystemFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            var collection = new KbQueryResultCollection<TData>();
            var result = collection.AddNew();

            result.AddField("Counter");
            result.AddField("Value");

            var counters = core.Health.CloneCounters();

            foreach (var counter in counters)
            {
                var values = new List<TData>
                {
                    Text.SeperateCamelCase(counter.Key).CastToT<TData> (EngineCore<TData>.StrCast),
                    counter.Value.Value.ToString("n0").CastToT<TData> (EngineCore<TData>.StrCast)
                };

                result.AddRow(values);
            }

            return collection;
        }
    }
}
