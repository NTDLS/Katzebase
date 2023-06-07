namespace Katzebase.Engine.Documents.Threading.SingleSchemaQuery
{
    internal class DocumentThreadingConstants
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
