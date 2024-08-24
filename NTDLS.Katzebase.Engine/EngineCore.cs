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
        public HealthManager Health;
        public SessionManager Sessions;
        public ProcedureManager Procedures;
        public IndexManager Indexes;
        public QueryManager Query;
        public ThreadPoolManager ThreadPool;

        internal OptimisticSemaphore LockManagementSemaphore { get; private set; } = new();

        public EngineCore(KatzebaseSettings settings)
        {
            Settings = settings;

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var fileVersion = string.Join(".", (fileVersionInfo.FileVersion?.Split('.').Take(3)).EnsureNotNull());

            LogManager.Information($"{fileVersionInfo.ProductName} v{fileVersion} PID:{System.Environment.ProcessId}");

            LogManager.Information("Creating log directory.");
            Directory.CreateDirectory(Settings.LogDirectory);

            LogManager.Information("Initializing cache manager.");
            Cache = new CacheManager(this);

            LogManager.Information("Initializing IO manager.");
            IO = new IOManager(this);

            LogManager.Information("Initializing health manager.");
            Health = new HealthManager(this);

            LogManager.Information("Initializing environment manager.");
            Environment = new EnvironmentManager(this);

            LogManager.Information("Initializing index manager.");
            Indexes = new IndexManager(this);

            LogManager.Information("Initializing session manager.");
            Sessions = new SessionManager(this);

            LogManager.Information("Initializing lock manager.");
            Locking = new LockManager(this);

            LogManager.Information("Initializing transaction manager.");
            Transactions = new TransactionManager(this);

            LogManager.Information("Initializing schema manager.");
            Schemas = new SchemaManager(this);

            LogManager.Information("Initializing document manager.");
            Documents = new DocumentManager(this);

            LogManager.Information("Initializing query manager.");
            Query = new QueryManager(this);

            LogManager.Information("Initializing thread pool manager.");
            ThreadPool = new ThreadPoolManager(this);

            LogManager.Information("Initializing procedure manager.");
            Procedures = new ProcedureManager(this);
        }

        public void Start()
        {
            LogManager.Information("Starting server engine.");

            LogManager.Information("Starting recovery.");
            Transactions.Recover();
            LogManager.Information("Recovery complete.");
        }

        public void Stop()
        {
            LogManager.Information("Stopping server engine.");

            ThreadPool.Stop();
            Cache.Close();
            Health.Close();
        }
    }
}
