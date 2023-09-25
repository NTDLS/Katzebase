using NTDLS.Katzebase.Engine.Atomicity.Management;
using NTDLS.Katzebase.Engine.Caching.Management;
using NTDLS.Katzebase.Engine.Documents.Management;
using NTDLS.Katzebase.Engine.Functions.Management;
using NTDLS.Katzebase.Engine.Health.Management;
using NTDLS.Katzebase.Engine.Heartbeat.Management;
using NTDLS.Katzebase.Engine.Indexes.Management;
using NTDLS.Katzebase.Engine.IO;
using NTDLS.Katzebase.Engine.Locking;
using NTDLS.Katzebase.Engine.Logging;
using NTDLS.Katzebase.Engine.Query.Management;
using NTDLS.Katzebase.Engine.Schemas.Management;
using NTDLS.Katzebase.Engine.Sessions.Management;
using NTDLS.Katzebase.Shared;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Caching;

namespace NTDLS.Katzebase.Engine
{
    public class Core
    {
        internal IOManager IO;
        internal LockManager Locking;
        internal CacheManager Cache;
        internal KatzebaseSettings Settings;
        internal HeartbeatManager Heartbeat;

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

        public MemoryCache LookupOptimizationCache { get; set; } = new MemoryCache("ConditionLookupOptimization");

        public Core(KatzebaseSettings settings)
        {
            this.Settings = settings;

            Log = new LogManager(this);

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            Log.Write($"{fileVersionInfo.ProductName} v{fileVersionInfo.ProductVersion} PID:{Process.GetCurrentProcess().Id}");

            Log.Write("Initializing cache manager.");
            Cache = new CacheManager(this);

            Log.Write("Initializing IO manager.");
            IO = new IOManager(this);

            Log.Write("Initializing health manager.");
            Health = new HealthManager(this);

            Log.Write("Initializing environment manager.");
            Environment = new EnvironmentManager(this);

            Log.Write("Initializing index manager.");
            Indexes = new IndexManager(this);

            Log.Write("Initializing session manager.");
            Sessions = new SessionManager(this);

            Log.Write("Initializing lock manager.");
            Locking = new LockManager(this);

            Log.Write("Initializing transaction manager.");
            Transactions = new TransactionManager(this);

            Log.Write("Initializing schema manager.");
            Schemas = new SchemaManager(this);

            Log.Write("Initializing document manager.");
            Documents = new DocumentManager(this);

            Log.Write("Initializing query manager.");
            Query = new QueryManager(this);

            Log.Write("Initializing procedure manager.");
            Procedures = new ProcedureManager(this);

            Log.Write("Initializing hearbeat.");
            Heartbeat = new HeartbeatManager(this);
        }

        public void Start()
        {
            Log.Write("Starting the server.");

            Log.Write("Starting recovery.");
            Transactions.Recover();
            Log.Write("Recovery complete.");

            Heartbeat.Start();
        }

        public void Stop()
        {
            Log.Write("Stopping the server.");

            Cache.Close();
            Heartbeat.Stop();
            Health.Close();
            Log.Close();
        }
    }
}
