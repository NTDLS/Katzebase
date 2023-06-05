using static Katzebase.Engine.Documents.Threading.DocumentThreadingConstants;

namespace Katzebase.Engine.Documents.Threading
{
    internal class DocumentLookupThreadSlot
    {
        public Exception? Exception { get; set; } = null;
        public int ThreadSlotNumber { get; set; }
        public PersistDocumentCatalogItem DocumentCatalogItem { get; set; } = new PersistDocumentCatalogItem();
        public DocumentLookupThreadState State { get; set; } = DocumentLookupThreadState.Initializing;
        public AutoResetEvent Event { get; set; } = new AutoResetEvent(false);
        public DocumentLookupThreadSlot(int threadSlotNumber)
        {
            ThreadSlotNumber = threadSlotNumber;
        }
    }
}
