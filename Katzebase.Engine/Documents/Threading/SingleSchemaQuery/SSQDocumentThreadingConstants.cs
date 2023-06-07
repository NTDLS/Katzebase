namespace Katzebase.Engine.Documents.Threading.SingleSchemaQuery
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
