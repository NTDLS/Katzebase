using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemClearCacheAllocations
    {
        public static KbQueryResultCollection Execute<TData>(EngineCore<TData> core, Transaction<TData> transaction, SystemFunctionParameterValueCollection function) where TData : IStringable
        {
            core.Cache.Clear();
            return new KbQueryResultCollection();
        }
    }
}
