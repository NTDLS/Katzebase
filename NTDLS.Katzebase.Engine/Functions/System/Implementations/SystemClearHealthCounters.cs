using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Interfaces;
using NTDLS.Katzebase.Parsers.Functions.System;
namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    public static class SystemClearHealthCounters
    {
        public static KbQueryResultCollection<TData> Execute<TData>(EngineCore<TData> core, Transaction<TData> transaction, SystemFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            core.Health.ClearCounters();
            return new KbQueryResultCollection<TData>();
        }
    }
}
