namespace NTDLS.Katzebase.Shared
{
    public class KatzebaseSettings
    {
        /// <summary>
        /// If true, the all applicable IO operations will be cached on read and write.
        /// </summary>
        public bool CacheEnabled { get; set; } = true;
        /// <summary>
        /// The maximum amount of memory in megabytes that the server will be allowed to use before scavenging the cache.
        /// </summary>
        public int CacheMaxMemoryMegabytes { get; set; } = 4096;

        /// <summary>
        /// The number of memory cache partitions to create. (0 = CPU Count)
        /// </summary>
        public int CachePartitions { get; set; } = 0;

        /// <summary>
        /// The number of seconds between cache scavenge operations. This is when the cache manager enforces memory limits by ejecting
        /// lesser used cache items.
        /// </summary>
        public int CacheScavengeInterval { get; set; } = 10;

        /// <summary>
        /// The number of seconds to keep an item in cache (sliding expiration).
        /// </summary>
        public int CacheSeconds { get; set; } = 3600;

        /// <summary>
        /// The number of documents to be stored per file in the schema. When documents are needed from the disk, the entire page will be read.
        /// The right number strikes the balance between disk trashing and optimal disk reads. This is also the minimum locking granularity.
        /// </summary>
        public uint DefaultDocumentPageSize { get; set; } = 100;

        /// <summary>
        /// Number of seconds between operations that check for low server activity before performing LOH compaction. (0 = disabled)
        /// </summary>
        public int LargeObjectHeapCompactionInterval { get; set; } = 60;

        /// <summary>
        /// The number of index partitions to create when the partition count is unspecified at index creation.
        /// </summary>
        public uint DefaultIndexPartitions { get; set; } = 100;

        /// <summary>
        /// The number of threads to allocate to the indexing thread pool.
        /// </summary>
        public int IndexingThreadPoolSize { get; set; } = 0;

        /// <summary>
        /// The maximum number of items to queue in the thread pool.
        /// </summary>
        public int IndexingThreadPoolQueueDepth { get; set; } = 10000;

        /// <summary>
        /// The maximum number of items to queue in each child thread pool per operation.
        /// Higher values can increase memory pressure and greatly increase the duration of transaction cancelation.
        /// </summary>
        public int IndexingChildThreadPoolQueueDepth { get; set; } = 100;

        /// <summary>
        /// The number of threads to allocate to the thread pool.
        /// </summary>
        public int LookupThreadPoolSize { get; set; } = 0;

        /// <summary>
        /// The maximum number of items to queue in the thread pool.
        /// </summary>
        public int LookupThreadPoolQueueDepth { get; set; } = 10000;

        /// <summary>
        /// The maximum number of items to queue in each child thread pool per operation.
        /// Higher values can increase memory pressure and greatly increase the duration of transaction cancelation.
        /// </summary>
        public int LookupChildThreadPoolQueueDepth { get; set; } = 100;

        /// <summary>
        /// The number of threads to allocate to the thread pool.
        /// </summary>
        public int IntersectionThreadPoolSize { get; set; } = 0;

        /// <summary>
        /// The maximum number of items to queue in the thread pool.
        /// </summary>
        public int IntersectionThreadPoolQueueDepth { get; set; } = 10000;

        /// <summary>
        /// The maximum number of items to queue in each child thread pool per operation.
        /// Higher values can increase memory pressure and greatly increase the duration of transaction cancelation.
        /// </summary>
        public int IntersectionChildThreadPoolQueueDepth { get; set; } = 100;


        /// <summary>
        /// The number of threads to allocate to the thread pool.
        /// </summary>
        public int MaterializationThreadPoolSize { get; set; } = 0;

        /// <summary>
        /// The maximum number of items to queue in the thread pool.
        /// </summary>
        public int MaterializationThreadPoolQueueDepth { get; set; } = 10000;

        /// <summary>
        /// The maximum number of items to queue in each child thread pool per operation.
        /// Higher values can increase memory pressure and greatly increase the duration of transaction cancelation.
        /// </summary>
        public int MaterializationChildThreadPoolQueueDepth { get; set; } = 100;

        /// <summary>
        /// Whether the engine will keep health metrics.
        /// </summary>
        public bool HealthMonitoringEnabled { get; set; } = true;

        /// <summary>
        /// Whether the engine will keep instance level health metrics. This can be useful but will have a serious impact on performance.
        /// Must also enable [HealthMonitoringEnabled].
        /// </summary>
        public bool HealthMonitoringInstanceLevelEnabled { get; set; } = false;

        /// <summary>
        /// The total number of seconds that instance level counters should stay in the health monitor for observation.
        /// </summary>
        public int HealthMonitoringInstanceLevelTimeToLiveSeconds { get; set; } = 600;

        /// <summary>
        /// The maximum number of seconds to wait for a transaction to acquire a lock before timing out. (0 = infinite)
        /// </summary>
        public int LockWaitTimeoutSeconds { get; set; } = 0;

        /// <summary>
        /// The number of seconds between writing health statistics to disk and trimming any instance level counters.
        /// </summary>
        public int HealthMonitoringCheckpointSeconds { get; set; } = 600;

        /// <summary>
        /// The TCP/IP listen port for the server.
        /// </summary>
        public int ListenPort { get; set; } = 6858;

        /// <summary>
        /// The top level directory for all schemas.
        /// </summary>
        public string DataRootPath
        {
            get => dataRootPath;
            set
            {
                dataRootPath = value.TrimEnd(['/', '\\']).Trim();
                if (Path.IsPathRooted(dataRootPath) == false)
                {
                    dataRootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dataRootPath));
                }
            }
        }
        private string dataRootPath = string.Empty;

        /// <summary>
        /// The directory where transaction logs are stored.
        /// </summary>
        public string TransactionDataPath
        {
            get => transactionDataPath;
            set
            {
                transactionDataPath = value.TrimEnd(['/', '\\']).Trim();

                if (Path.IsPathRooted(dataRootPath) == false)
                {
                    transactionDataPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, transactionDataPath));
                }
            }
        }

        private string transactionDataPath = string.Empty;

        /// <summary>
        /// The directory where text and performance logs are stores.
        /// </summary>
        public string LogDirectory
        {
            get => logDirectory;
            set
            {
                logDirectory = value.TrimEnd(['/', '\\']).Trim();
                if (Path.IsPathRooted(dataRootPath) == false)
                {
                    logDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logDirectory));
                }
            }
        }
        private string logDirectory = string.Empty;

        /// <summary>
        /// If true, text logs will be flushed at every write. This ensures that the log file is always up-to-date on disk.
        /// </summary>
        public bool FlushLog { get; set; }

        /// <summary>
        /// When true, all write operations associated with a transaction are deferred until the transaction is committed.
        /// </summary>
        public bool DeferredIOEnabled { get; set; }

        /// <summary>
        /// Causes the server to write super-verbose information about almost every internal operation.
        /// </summary>
        public bool WriteTraceData { get; set; }
    }
}
