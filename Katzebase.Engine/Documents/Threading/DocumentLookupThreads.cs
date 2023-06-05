using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using static Katzebase.Engine.Documents.Threading.DocumentThreadingConstants;

namespace Katzebase.Engine.Documents.Threading
{
    internal class DocumentLookupThreads
    {
        public bool HasExcepted { get; set; }
        public DocumentLookupResults Results { get; private set; } = new();
        public List<Thread> Threads { get; private set; } = new();
        public List<DocumentLookupThreadSlot> Slots { get; private set; } = new();
        public int MaxThreads { get; private set; }

        private Transaction transaction { get; set; }
        private PersistSchema schemaMeta { get; set; }
        private PreparedQuery query { get; set; }
        private ConditionLookupOptimization lookupOptimization { get; set; }
        private ParameterizedThreadStart threadProc { get; set; }

        public DocumentLookupThreads(Transaction transaction, PersistSchema schemaMeta, PreparedQuery query,
            ConditionLookupOptimization lookupOptimization, ParameterizedThreadStart threadProc)
        {
            this.transaction = transaction;
            this.schemaMeta = schemaMeta;
            this.query = query;
            this.lookupOptimization = lookupOptimization;
            this.threadProc = threadProc;
        }

        public void Stop()
        {
            WaitOnThreadCompletion();

            for (int i = 0; i < MaxThreads; i++)
            {
                Slots[i].State = DocumentLookupThreadState.Shutdown;
                Slots[i].Event.Set();
            }
        }

        public void InitializePool(int maxThreads)
        {
            MaxThreads = maxThreads;

            for (int i = 0; i < maxThreads; i++)
            {
                Slots.Add(new DocumentLookupThreadSlot(i));
                var param = new DocumentLookupThreadParam(transaction, schemaMeta, query, lookupOptimization, Slots, i, Results);
                var thread = new Thread(threadProc);
                thread.Start(param);
                Threads.Add(thread);
            }
        }

        public void Enqueue(PersistDocumentCatalogItem documentCatalogItem)
        {
            var threadSlot = GetReadyThread();
            if (threadSlot != null)
            {
                threadSlot.DocumentCatalogItem = documentCatalogItem;
                threadSlot.Event.Set();
            }
        }

        public DocumentLookupThreadSlot? GetReadyThread()
        {
            Utility.EnsureNotNull(Slots);

            while (true)
            {
                for (int i = 0; i < MaxThreads; i++)
                {
                    if (Slots[i].State == DocumentLookupThreadState.Shutdown)
                    {
                        return null;
                    }

                    if (Slots[i].State == DocumentLookupThreadState.Exception)
                    {
                        HasExcepted = true;
                        return null;
                    }

                    if (Slots[i].State == DocumentLookupThreadState.Ready)
                    {
                        Slots[i].State = DocumentLookupThreadState.Queued;
                        return Slots[i];
                    }
                }
                Thread.Sleep(1);
            }
        }

        public void WaitOnThreadCompletion()
        {
            Utility.EnsureNotNull(Slots);

            bool stillWaiting = true;

            while (stillWaiting)
            {
                stillWaiting = false;
                for (int i = 0; i < MaxThreads; i++)
                {
                    if (Slots[i].State != DocumentLookupThreadState.Ready
                        && Slots[i].State != DocumentLookupThreadState.Exception)
                    {
                        Thread.Sleep(1);
                        stillWaiting = true;
                        break;
                    }
                }
            }
        }
    }
}
