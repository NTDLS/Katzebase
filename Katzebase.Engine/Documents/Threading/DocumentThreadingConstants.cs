namespace Katzebase.Engine.Documents.Threading
{
    internal class DocumentThreadingConstants
    {
        internal enum DocumentLookupThreadState
        {
            Initializing,
            Ready,
            Queued,
            Executing,
            Shutdown
        }
    }
}
