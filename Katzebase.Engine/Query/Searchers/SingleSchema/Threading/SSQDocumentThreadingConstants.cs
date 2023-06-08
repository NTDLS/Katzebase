namespace Katzebase.Engine.Query.Searchers.SingleSchema.Threading
{
    internal class SSQDocumentThreadingConstants
    {
        internal enum DocumentLookupThreadState
        {
            Initializing,
            Ready,
            Queued,
            Executing,
            Exception,
            Shutdown
        }
    }
}
