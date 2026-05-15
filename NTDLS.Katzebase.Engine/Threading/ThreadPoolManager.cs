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
                MaximumThreadCount = _core.Settings.IndexingThreadPoolMaximumSize <= 0 ? (Environment.ProcessorCount * 2) : _core.Settings.IndexingThreadPoolMaximumSize,
                InitialThreadCount = _core.Settings.IndexingThreadPoolMaximumSize <= 0 ? 2 : Math.Min(_core.Settings.IndexingThreadPoolMaximumSize / 2, 2),
                MaximumQueueDepth = _core.Settings.IndexingThreadPoolQueueDepth
            });

            Lookup = new DelegateThreadPool(new DelegateThreadPoolConfiguration()
            {
                MaximumThreadCount = _core.Settings.LookupThreadPoolMaximumSize <= 0 ? (Environment.ProcessorCount * 2) : _core.Settings.LookupThreadPoolMaximumSize,
                InitialThreadCount = _core.Settings.LookupThreadPoolMaximumSize <= 0 ? 2 : Math.Min(_core.Settings.LookupThreadPoolMaximumSize / 2, 2),
                MaximumQueueDepth = _core.Settings.LookupThreadPoolQueueDepth
            });

            Intersection = new DelegateThreadPool(new DelegateThreadPoolConfiguration()
            {
                MaximumThreadCount = _core.Settings.IntersectionThreadPoolMaximumSize <= 0 ? (Environment.ProcessorCount * 2) : _core.Settings.IntersectionThreadPoolMaximumSize,
                InitialThreadCount = _core.Settings.IntersectionThreadPoolMaximumSize <= 0 ? 2 : Math.Min(_core.Settings.IntersectionThreadPoolMaximumSize / 2, 2),
                MaximumQueueDepth = _core.Settings.IntersectionThreadPoolQueueDepth
            });

            Materialization = new DelegateThreadPool(new DelegateThreadPoolConfiguration()
            {
                MaximumThreadCount = _core.Settings.MaterializationThreadPoolMaximumSize <= 0 ? (Environment.ProcessorCount * 2) : _core.Settings.MaterializationThreadPoolMaximumSize,
                InitialThreadCount = _core.Settings.MaterializationThreadPoolMaximumSize <= 0 ? 2 : Math.Min(_core.Settings.MaterializationThreadPoolMaximumSize / 2, 2),
                MaximumQueueDepth = _core.Settings.MaterializationThreadPoolQueueDepth
            });
        }

        public void Stop()
        {
            Indexing.Dispose();
            Lookup.Dispose();
            Intersection.Dispose();
            Materialization.Dispose();
        }
    }
}
