using NTDLS.DelegateThreadPooling;

namespace NTDLS.Katzebase.Engine.Threading.Management
{
    /// <summary>
    /// Public class that houses thread pools for various operations.
    /// </summary>
    public class ThreadPoolManager
    {
        public DelegateThreadPool Generic { get; set; }

        private readonly EngineCore _core;

        public ThreadPoolManager(EngineCore core)
        {
            _core = core;

            Generic = new DelegateThreadPool(_core.Settings.InitialThreadPoolSize);
        }

        public void Stop()
        {
            Generic.Stop();
        }
    }
}
