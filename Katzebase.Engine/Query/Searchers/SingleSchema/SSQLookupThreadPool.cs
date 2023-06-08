using Katzebase.Engine.Documents;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PrivateLibrary;

namespace Katzebase.Engine.Query.Searchers.SingleSchema
{
    internal class SSQLookupThreadPool
    {
        private List<Thread> _threads = new();
        private int _runningThreadCount = 0;

        //Whatever excletion the one of the threads threw.
        public Exception? Exception { get; set; } = null;
        public bool HasException => Exception != null;

        public SSQDocumentLookupResults Results = new();
        public FixedSizeWaitQueue<PersistDocumentCatalogItem> Queue { get; set; } = new(100);
        public PersistSchema SchemaMeta { get; private set; }
        public Core Core { get; private set; }
        public PerformanceTrace? PT { get; private set; }
        public Transaction Transaction { get; private set; }
        public PreparedQuery Query { get; private set; }
        public ConditionLookupOptimization? LookupOptimization { get; private set; }
        public bool ContinueToProcessQueue { get; private set; } = true;

        public SSQLookupThreadPool(Core core, PerformanceTrace? pt, Transaction transaction,
            PersistSchema schemaMeta, PreparedQuery query, ConditionLookupOptimization? lookupOptimization = null)
        {
            Core = core;
            SchemaMeta = schemaMeta;
            PT = pt;
            Transaction = transaction;
            Query = query;
            LookupOptimization = lookupOptimization;
        }
        public void IncrementRunningThreadCount()
        {
            lock (this)
            {
                _runningThreadCount++;
            }
        }

        public void DecrementRunningThreadCount()
        {
            lock (this)
            {
                _runningThreadCount--;
            }
        }

        public void Start(ParameterizedThreadStart threadProc, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var thread = new Thread(threadProc);
                _threads.Add(thread);

                IncrementRunningThreadCount();

                thread.Start(this);
            }
        }

        /// <summary>
        /// Call when all items have been queued and we just want to wait on the queue to empty then the threads to finish.
        /// </summary>
        public void WaitForCompletion()
        {
            while (Queue.Count > 0 && HasException == false)
            {
                Thread.Sleep(1);
            }

            ContinueToProcessQueue = false;

            Queue.Stop();

            while (_runningThreadCount > 0)
            {
                Thread.Sleep(1);
            }

            if (Exception != null)
            {
                throw Exception;
            }
        }
    }
}
