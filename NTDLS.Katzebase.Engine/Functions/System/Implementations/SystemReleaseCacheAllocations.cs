using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;
using System.Runtime;

namespace NTDLS.Katzebase.Engine.Functions.System.Implementations
{
    internal static class SystemReleaseCacheAllocations
    {
        public static KbQueryResultCollection<TData> Execute<TData>(EngineCore<TData> core, Transaction<TData> transaction, SystemFunctionParameterValueCollection<TData> function) where TData : IStringable
        {
            //GC.Collect(); //before b3699d63a3337d936e302bcc8fa746376f02b317
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce; //after b3699d63a3337d936e302bcc8fa746376f02b317
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true); //after b3699d63a3337d936e302bcc8fa746376f02b317
            return new KbQueryResultCollection<TData>();
        }
    }
}
