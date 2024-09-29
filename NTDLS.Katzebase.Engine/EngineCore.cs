using NTDLS.Helpers;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Threading.Management;
using NTDLS.Katzebase.Shared;
using NTDLS.Semaphore;
using System.Diagnostics;
using System.Reflection;

namespace NTDLS.Katzebase.Engine
{

    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this IStringable value)
        {
            if (value == null) return true;
            return value.IsNullOrEmpty();
        }
        public static T? ParseToT<T>(this string value, Func<string, T> parse)
        {
            if (value == null) return default(T);
            return parse(value);
        }

        public static T? CastToT<T>(this string value, Func<string, T> cast)
        {
            if (value == null) return default(T);
            return cast(value);
        }
        public static IStringable? Empty => null;
    }
    public interface IStringable
    {
        bool IsNullOrEmpty();
        IStringable ToLowerInvariant();
        string GetKey();
        //char[] ToCharArr();
        //Func<string, IStringable?> Converter { get; }
        T ToT<T>();
        object ToT(Type t);
        T ToNullableT<T>();
    }

    public class EngineCore<TData> where TData : IStringable
    {
        static public Func<string, TData>? StrCast;
        static public Func<string, TData>? StrParse;
        static public Func<TData, TData, int>? Compare;
        static public object Locker = new object();
        internal IOManager<TData> IO;
        internal LockManager<TData> Locking;
        internal CacheManager<TData> Cache;
        internal KatzebaseSettings Settings;

        public SchemaManager<TData> Schemas;
        public EnvironmentManager<TData> Environment;
        public DocumentManager<TData> Documents;
        public TransactionManager<TData> Transactions;
        public HealthManager<TData> Health;
        public SessionManager<TData> Sessions;
        public ProcedureManager<TData> Procedures;
        public IndexManager<TData> Indexes;
        public QueryManager<TData> Query;
        public ThreadPoolManager<TData> ThreadPool;
        internal OptimisticSemaphore LockManagementSemaphore { get; private set; } = new();

        public EngineCore(KatzebaseSettings settings, Func<string, TData>? cast, Func<string, TData>? parse, Func<TData, TData, int>? compare)
        {
            lock (Locker)
            {
                if (cast == null)
                {
                    StrCast = str => (TData)(object)str;
                }
                else
                {
                    StrCast = cast;
                }
            }

            lock (Locker)
            {
                if (cast == null)
                {
                    StrParse = str => (TData)(object)str;
                }
                else
                {
                    StrParse = parse;
                }
            }

            lock (Locker)
            {
                if (cast == null)
                {
                    Compare = (x, y) => string.Compare(x.ToT<string>(), y.ToT<string>());
                }
                else
                {
                    Compare = compare;
                }
            }
            Settings = settings;

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var fileVersion = string.Join(".", (fileVersionInfo.FileVersion?.Split('.').Take(3)).EnsureNotNull());

            LogManager.Information($"{fileVersionInfo.ProductName} v{fileVersion} PID:{System.Environment.ProcessId}");

            LogManager.Information("Creating log directory.");
            Directory.CreateDirectory(Settings.LogDirectory);

            LogManager.Information("Initializing cache manager.");
            Cache = new CacheManager<TData>(this);

            LogManager.Information("Initializing IO manager.");
            IO = new IOManager<TData>(this);

            LogManager.Information("Initializing health manager.");
            Health = new HealthManager<TData>(this);

            LogManager.Information("Initializing environment manager.");
            Environment = new EnvironmentManager<TData>(this);

            LogManager.Information("Initializing index manager.");
            Indexes = new IndexManager<TData>(this);

            LogManager.Information("Initializing session manager.");
            Sessions = new SessionManager<TData>(this);

            LogManager.Information("Initializing lock manager.");
            Locking = new LockManager<TData>(this);

            LogManager.Information("Initializing transaction manager.");
            Transactions = new TransactionManager<TData>(this);

            LogManager.Information("Initializing schema manager.");
            Schemas = new SchemaManager<TData>(this);

            LogManager.Information("Initializing document manager.");
            Documents = new DocumentManager<TData>(this);

            LogManager.Information("Initializing query manager.");
            Query = new QueryManager<TData>(this);

            LogManager.Information("Initializing thread pool manager.");
            ThreadPool = new ThreadPoolManager<TData>(this);

            LogManager.Information("Initializing procedure manager.");
            Procedures = new ProcedureManager<TData>(this);

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
            Cache.Close();

            LogManager.Information("Stopping health manager.");
            Health.Close();
        }
    }
}
