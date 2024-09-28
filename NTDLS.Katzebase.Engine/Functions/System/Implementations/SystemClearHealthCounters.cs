using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemClearHealthCounters
    {
        public static KbQueryResultCollection Execute<TData>(EngineCore<TData> core, Transaction<TData> transaction, SystemFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            core.Health.ClearCounters();
            return new KbQueryResultCollection();
        }
    }
}
