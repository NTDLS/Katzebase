using Katzebase.Engine.Atomicity.Management;
using Katzebase.Engine.Caching.Management;
using Katzebase.Engine.Documents.Management;
using Katzebase.Engine.Functions.Management;
using Katzebase.Engine.Health.Management;
using Katzebase.Engine.Heartbeat.Management;
using Katzebase.Engine.Indexes.Management;
using Katzebase.Engine.IO;
using Katzebase.Engine.Locking;
using Katzebase.Engine.Logging;
using Katzebase.Engine.Query.Management;
using Katzebase.Engine.Schemas.Management;
using Katzebase.Engine.Sessions.Management;
using Katzebase.PrivateLibrary;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Caching;

namespace Katzebase.Engine
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
