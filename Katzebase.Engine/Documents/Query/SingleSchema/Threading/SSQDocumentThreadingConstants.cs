namespace Katzebase.Engine.Documents.Query.SingleSchema.Threading
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
