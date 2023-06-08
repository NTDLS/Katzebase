using Katzebase.Engine.Documents;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using Katzebase.PrivateLibrary;
using System.Threading;

namespace Katzebase.Engine.Query.Searchers.SingleSchema.Threading
{
    internal class SSQNoConditionLookupThreadPool
    {
        private List<Thread> _threads = new();
        private int _runningThreadCount = 0;

        //Whatever excletion the one of the threads threw.
        public Exception? Exception { get; set; } = null;
        public bool HasException => Exception != null;

        public SSQDocumentLookupResults Results = new();
        public FixedSizeWaitQueue<PersistDocumentCatalogItem> Queue { get; set; } = new(100);
        public PersistSchema SchemaMeta { get; set; }
        public Core Core { get; set; }
        public PerformanceTrace? PT { get; set; }
        public Transaction Transaction { get; set; }
        public PreparedQuery Query { get; set; }
        public bool ContinueToProcessQueue { get; private set; } = true;

        public SSQNoConditionLookupThreadPool(Core core, PerformanceTrace? pt, Transaction transaction, PersistSchema schemaMeta, PreparedQuery query)
        {
            Core = core;
            SchemaMeta = schemaMeta;
            PT = pt;
            Transaction = transaction;
            Query = query;
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
