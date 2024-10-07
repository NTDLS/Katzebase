using NTDLS.DelegateThreadPooling;

namespace NTDLS.Katzebase.Engine.Threading
{
    /// <summary>
    /// Public class that houses thread pools for various operations.
    /// </summary>
    public class ThreadPoolManager
    {
        public DelegateThreadPool Lookup { get; set; }
        public DelegateThreadPool Indexing { get; set; }
        public DelegateThreadPool Intersection { get; set; }
        public DelegateThreadPool Materialization { get; set; }

        private readonly EngineCore _core;

        public ThreadPoolManager(EngineCore core)
        {
            _core = core;

            Indexing = new DelegateThreadPool(_core.Settings.IndexingThreadPoolSize <= 0 ? Environment.ProcessorCount : _core.Settings.IndexingThreadPoolSize, _core.Settings.IndexingThreadPoolQueueDepth);
            Lookup = new DelegateThreadPool(_core.Settings.LookupThreadPoolSize <= 0 ? Environment.ProcessorCount : _core.Settings.LookupThreadPoolSize, _core.Settings.LookupThreadPoolQueueDepth);
            Intersection = new DelegateThreadPool(_core.Settings.IntersectionThreadPoolSize <= 0 ? Environment.ProcessorCount : _core.Settings.IntersectionThreadPoolSize, _core.Settings.IntersectionThreadPoolQueueDepth);
            Materialization = new DelegateThreadPool(_core.Settings.MaterializationThreadPoolSize <= 0 ? Environment.ProcessorCount : _core.Settings.MaterializationThreadPoolSize, _core.Settings.MaterializationThreadPoolQueueDepth);
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
