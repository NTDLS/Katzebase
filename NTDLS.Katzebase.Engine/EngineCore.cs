using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Threading;
using NTDLS.Katzebase.Shared;
using NTDLS.Semaphore;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static NTDLS.Katzebase.Api.KbConstants;

[assembly: InternalsVisibleTo("NTDLS.Katzebase.Engine.Tests")]

namespace NTDLS.Katzebase.Engine
{
    public class EngineCore
    {
        internal IOManager IO;
        internal LockManager Locking;
        internal CacheManager Cache;
        internal KatzebaseSettings Settings;

        public PolicyManager Policy;
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

        /// <summary>
        /// Tokens that will be replaced by literal values by the tokenizer.
        /// </summary>
        public KbInsensitiveDictionary<KbVariable> GlobalConstants { get; private set; } = new();

        internal OptimisticSemaphore LockManagementSemaphore { get; private set; } = new();

        public EngineCore(KatzebaseSettings settings)
        {
#if DEBUG
            ThreadLockOwnershipTracking.Enable(); //NTDLS.Semaphore
#endif

            Settings = settings;

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var fileVersion = string.Join(".", (fileVersionInfo.FileVersion?.Split('.').Take(3)).EnsureNotNull());

            //Define all query literal constants here, these will be filled in my the tokenizer. Do not use quotes for strings.
            GlobalConstants.Add("true", new("1", KbBasicDataType.Numeric) { IsConstant = true });
            GlobalConstants.Add("false", new("0", KbBasicDataType.Numeric) { IsConstant = true });
            GlobalConstants.Add("null", new(null, KbBasicDataType.Undefined) { IsConstant = true });

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

            LogManager.Information("Initializing policy manager.");
            Policy = new PolicyManager(this);

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

            Schemas.PostInitialization();
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
            LogManager.Information("Stopping thread pool.");
            ThreadPool.Stop();

            LogManager.Information("Stopping cache manager.");
            Cache.Stop();

            LogManager.Information("Stopping health manager.");
            Health.Stop();
        }
    }
}
