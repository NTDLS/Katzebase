using NTDLS.DelegateThreadPooling;

namespace NTDLS.Katzebase.Engine.Threading
{
    /// <summary>
    /// Public class that houses thread pools for various operations.
    /// </summary>
    public class ThreadPoolManager
    {
        public DelegateThreadPool Lookup { get; private set; }
        public DelegateThreadPool Indexing { get; private set; }
        public DelegateThreadPool Intersection { get; private set; }
        public DelegateThreadPool Materialization { get; private set; }

        private readonly EngineCore _core;

        public ThreadPoolManager(EngineCore core)
        {
            _core = core;

            Indexing = new DelegateThreadPool(new DelegateThreadPoolConfiguration()
            {
                InitialThreadCount = 2,
                MaximumThreadCount = _core.Settings.IndexingThreadPoolSize <= 0 ? (Environment.ProcessorCount * 2) : _core.Settings.IndexingThreadPoolSize,
                MaximumQueueDepth = _core.Settings.IndexingThreadPoolQueueDepth
            });

            Lookup = new DelegateThreadPool(new DelegateThreadPoolConfiguration()
            {
                InitialThreadCount = 2,
                MaximumThreadCount = _core.Settings.LookupThreadPoolSize <= 0 ? (Environment.ProcessorCount * 2) : _core.Settings.LookupThreadPoolSize,
                MaximumQueueDepth = _core.Settings.LookupThreadPoolQueueDepth
            });

            Intersection = new DelegateThreadPool(new DelegateThreadPoolConfiguration()
            {
                InitialThreadCount = 2,
                MaximumThreadCount = _core.Settings.IntersectionThreadPoolSize <= 0 ? (Environment.ProcessorCount * 2) : _core.Settings.IntersectionThreadPoolSize,
                MaximumQueueDepth = _core.Settings.IntersectionThreadPoolQueueDepth
            });

            Materialization = new DelegateThreadPool(new DelegateThreadPoolConfiguration()
            {
                InitialThreadCount = 2,
                MaximumThreadCount = _core.Settings.MaterializationThreadPoolSize <= 0 ? (Environment.ProcessorCount * 2) : _core.Settings.MaterializationThreadPoolSize,
                MaximumQueueDepth = _core.Settings.MaterializationThreadPoolQueueDepth
            });
        }

        public void Stop()
        {
            Indexing.Stop();
            Lookup.Stop();
            Intersection.Stop();
            Materialization.Stop();
        }
    }
}
