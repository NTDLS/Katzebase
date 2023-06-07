using static Katzebase.Engine.Documents.Query.SingleSchema.Threading.SSQDocumentThreadingConstants;

namespace Katzebase.Engine.Documents.Query.SingleSchema.Threading
{
    internal class SSQDocumentLookupThreadSlot
    {
        public Exception? Exception { get; set; } = null;
        public int ThreadSlotNumber { get; set; }
        public PersistDocumentCatalogItem DocumentCatalogItem { get; set; } = new PersistDocumentCatalogItem();
        public DocumentLookupThreadState State { get; set; } = DocumentLookupThreadState.Initializing;
        public AutoResetEvent Event { get; set; } = new AutoResetEvent(false);
        public SSQDocumentLookupThreadSlot(int threadSlotNumber)
        {
            ThreadSlotNumber = threadSlotNumber;
        }
    }
}
