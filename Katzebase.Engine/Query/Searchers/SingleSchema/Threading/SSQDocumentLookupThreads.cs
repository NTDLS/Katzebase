using Katzebase.Engine.Documents;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PublicLibrary;
using static Katzebase.Engine.Query.Searchers.SingleSchema.Threading.SSQDocumentThreadingConstants;

namespace Katzebase.Engine.Query.Searchers.SingleSchema.Threading
{
    internal class SSQDocumentLookupThreads
    {
        public bool HasExcepted { get; set; }
        public SSQDocumentLookupResults Results { get; private set; } = new();
        public List<Thread> Threads { get; private set; } = new();
        public List<SSQDocumentLookupThreadSlot> Slots { get; private set; } = new();
        public int MaxThreads { get; private set; }

        private PerformanceTrace? pt;
        private Transaction transaction;
        private PersistSchema SchemaMeta;
        private PreparedQuery query;
        private ConditionLookupOptimization lookupOptimization;
        private ParameterizedThreadStart threadProc;
        private Core core;

        public SSQDocumentLookupThreads(Core core, PerformanceTrace? pt, Transaction transaction, PersistSchema schemaMeta, PreparedQuery query,
            ConditionLookupOptimization lookupOptimization, ParameterizedThreadStart threadProc)
        {
            this.core = core;
            this.transaction = transaction;
            SchemaMeta = schemaMeta;
            this.query = query;
            this.lookupOptimization = lookupOptimization;
            this.threadProc = threadProc;
            this.pt = pt;
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
                Slots.Add(new SSQDocumentLookupThreadSlot(i));
                var param = new SSQDocumentLookupThreadParam(core, pt, transaction, SchemaMeta, query, lookupOptimization, Slots, i, Results);
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

        public SSQDocumentLookupThreadSlot? GetReadyThread()
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
