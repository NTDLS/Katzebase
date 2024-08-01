using NTDLS.Helpers;
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

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var productVersion = string.Join(".", (fileVersionInfo.ProductVersion?.Split('.').Take(3)).EnsureNotNull());

            Log.Verbose($"{fileVersionInfo.ProductName} v{productVersion} PID:{System.Environment.ProcessId}");

            Log.Information("Initializing cache manager.");
            Cache = new CacheManager(this);

            Log.Information("Initializing IO manager.");
            IO = new IOManager(this);

            Log.Information("Initializing health manager.");
            Health = new HealthManager(this);

            Log.Information("Initializing environment manager.");
            Environment = new EnvironmentManager(this);

            Log.Information("Initializing index manager.");
            Indexes = new IndexManager(this);

            Log.Information("Initializing session manager.");
            Sessions = new SessionManager(this);

            Log.Information("Initializing lock manager.");
            Locking = new LockManager(this);

            Log.Information("Initializing transaction manager.");
            Transactions = new TransactionManager(this);

            Log.Information("Initializing schema manager.");
            Schemas = new SchemaManager(this);

            Log.Information("Initializing document manager.");
            Documents = new DocumentManager(this);

            Log.Information("Initializing query manager.");
            Query = new QueryManager(this);

            Log.Information("Initializing thread pool manager.");
            ThreadPool = new ThreadPoolManager(this);

            Log.Information("Initializing procedure manager.");
            Procedures = new ProcedureManager(this);

        }

        public void Start()
        {
            Log.Information("Starting the server.");

            Log.Information("Starting recovery.");
            Transactions.Recover();
            Log.Information("Recovery complete.");
        }

        public void Stop()
        {
            Log.Information("Stopping the server.");

            ThreadPool.Stop();
            Cache.Close();
            Health.Close();
        }
    }
}
