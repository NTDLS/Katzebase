using Katzebase.Engine.Caching;
using Katzebase.Engine.Documents;
using Katzebase.Engine.Health;
using Katzebase.Engine.Indexes;
using Katzebase.Engine.IO;
using Katzebase.Engine.Locking;
using Katzebase.Engine.Logging;
using Katzebase.Engine.Query;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Sessions;
using Katzebase.Engine.Transactions;
using Katzebase.Library;
using System.Diagnostics;
using System.Reflection;

namespace Katzebase.Engine
{
    public class Core
    {
        public SchemaManager Schemas;
        public IOManager IO;
        public LockManager Locking;
        public DocumentManager Documents;
        public TransactionManager Transactions;
        public Settings settings;
        public LogManager Log;
        public HealthManager Health;
        public SessionManager Sessions;
        public CacheManager Cache;
        public PersistIndexManager Indexes;
        public QueryManager Query;

        public Core(Settings settings)
        {
            this.settings = settings;

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

            Log.Write("Initializing index manager.");
            Indexes = new PersistIndexManager(this);

            Log.Write("Initializing session manager.");
            Sessions = new SessionManager(this);

            Log.Write("Initializing lock manager.");
            Locking = new LockManager(this);

            Log.Write("Initializing transaction manager.");
            Transactions = new TransactionManager(this);

            Log.Write("Initializing namespace manager.");
            Schemas = new SchemaManager(this);

            Log.Write("Initializing document manager.");
            Documents = new DocumentManager(this);

            Log.Write("Initializing query manager.");
            Query = new QueryManager(this);

            Log.Write("Initilization complete.");
        }

        public void Start()
        {
            Log.Write("Starting server.");

            Transactions.Recover();
        }

        public void Shutdown()
        {
            Log.Write("Shutting down server.");

            Health.Close();
            Log.Close();
        }
    }
}
