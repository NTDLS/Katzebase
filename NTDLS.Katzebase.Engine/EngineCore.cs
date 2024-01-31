using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Threading.Management;
using NTDLS.Katzebase.Shared;
using NTDLS.Semaphore;
using System.Diagnostics;
using System.Reflection;

namespace NTDLS.Katzebase.Engine
{
    public class EngineCore
    {
        internal IOManager IO;
        internal LockManager Locking;
        internal CacheManager Cache;
        internal KatzebaseSettings Settings;

        public SchemaManager Schemas;
        public EnvironmentManager Environment;
        public DocumentManager Documents;
        public TransactionManager Transactions;
        public LogManager Log;
        public HealthManager Health;
        public SessionManager Sessions;
        public ProcedureManager Procedures;
        public IndexManager Indexes;
        public QueryManager Query;
        public ThreadPoolManager ThreadPool;

        internal OptimisticSemaphore CriticalSectionLockManagement { get; private set; } = new();

        public EngineCore(KatzebaseSettings settings)
        {
            if (Debugger.IsAttached)
            {
                ThreadOwnershipTracking.Enable();
            }

            Settings = settings;

            Log = new LogManager(this);

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            Log.Verbose($"{fileVersionInfo.ProductName} v{fileVersionInfo.ProductVersion} PID:{Process.GetCurrentProcess().Id}");

            Log.Verbose("Initializing cache manager.");
            Cache = new CacheManager(this);

            Log.Verbose("Initializing IO manager.");
            IO = new IOManager(this);

            Log.Verbose("Initializing health manager.");
            Health = new HealthManager(this);

            Log.Verbose("Initializing environment manager.");
            Environment = new EnvironmentManager(this);

            Log.Verbose("Initializing index manager.");
            Indexes = new IndexManager(this);

            Log.Verbose("Initializing session manager.");
            Sessions = new SessionManager(this);

            Log.Verbose("Initializing lock manager.");
            Locking = new LockManager(this);

            Log.Verbose("Initializing transaction manager.");
            Transactions = new TransactionManager(this);

            Log.Verbose("Initializing schema manager.");
            Schemas = new SchemaManager(this);

            Log.Verbose("Initializing document manager.");
            Documents = new DocumentManager(this);

            Log.Verbose("Initializing query manager.");
            Query = new QueryManager(this);

            Log.Verbose("Initializing thread pool manager.");
            ThreadPool = new ThreadPoolManager(this);

            Log.Verbose("Initializing procedure manager.");
            Procedures = new ProcedureManager(this);

        }

        public void Start()
        {
            Log.Verbose("Starting the server.");

            Log.Verbose("Starting recovery.");
            Transactions.Recover();
            Log.Verbose("Recovery complete.");
        }

        public void Stop()
        {
            Log.Verbose("Stopping the server.");

            ThreadPool.Stop();
            Cache.Close();
            Health.Close();
            Log.Close();
        }
    }
}
