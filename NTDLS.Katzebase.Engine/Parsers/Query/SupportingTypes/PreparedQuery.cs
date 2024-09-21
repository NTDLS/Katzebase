using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes
{
    /// <summary>
    /// Contains a parsed query via StaticQueryParser.PrepareQuery();
    /// </summary>
    internal class PreparedQuery
    {
        public enum QueryAttribute
        {
            IndexName,
            IsUnique,
            ProcessId,
            IncludePhysicalPages,
            TargetSchema,
            Schema,
            ObjectName,
            Parameters,
            Batches,
            Partitions,
            PageSize,

            //----------Configuration (BEGIN) ----------
            BaseAddress,
            DataRootPath,
            TransactionDataPath,
            LogDirectory,
            FlushLog,
            DefaultDocumentPageSize,
            UseCompression,
            HealthMonitoringEnabled,
            HealthMonitoringCheckpointSeconds,
            HealthMonitoringInstanceLevelEnabled,
            HealthMonitoringInstanceLevelTimeToLiveSeconds,
            MaxIdleConnectionSeconds,
            DefaultIndexPartitions,
            DeferredIOEnabled,
            WriteTraceData,
            CacheEnabled,
            CacheMaxMemory,
            CacheScavengeInterval,
            CachePartitions,
            CacheSeconds
            //----------Configuration (END)----------
        }

        public List<string>? _dynamicSchemaFieldFilter;
        public SemaphoreSlim? DynamicSchemaFieldSemaphore { get; private set; }

        /// <summary>
        /// When this is non-null, it will cause the searcher methods to pull fields from all schemas,
        ///     unless it also contains a list of schema aliases, in which case they will be used to filter
        ///     which schemas we pull fields from.
        /// </summary>
        public List<string>? DynamicSchemaFieldFilter
        {
            get => _dynamicSchemaFieldFilter;
            set
            {
                DynamicSchemaFieldSemaphore ??= new(1);
                _dynamicSchemaFieldFilter = value;
            }
        }

        /// <summary>
        /// Contains the hash of the whole query text with all constants and variables removed.
        /// </summary>
        public string? Hash { get; set; }
        public QueryBatch Batch { get; private set; }
        public Dictionary<QueryAttribute, object> Attributes { get; private set; } = new();
        public List<QuerySchema> Schemas { get; private set; } = new();
        public QueryType QueryType { get; set; }
        public SubQueryType SubQueryType { get; set; }

        /// <summary>
        /// Used for creating indexes.
        /// </summary>
        public List<string> CreateIndexFields { get; set; } = new();

        #region Execute Statement.
        public string? ProcedureName { get; set; }

        public QueryFieldCollection? ProcedureParameters { get; set; }

        #endregion

        #region Shared statement components.
        public int RowLimit { get; set; }
        public int RowOffset { get; set; }
        public ConditionCollection Conditions { get; set; }

        #endregion

        #region Select Statement.

        public QueryFieldCollection SelectFields { get; set; }
        public QueryFieldCollection GroupFields { get; set; }
        public SortFields SortFields { get; set; } = new();

        #endregion

        #region Update Statement.

        public List<string> UpdateFieldNames { get; set; } = new();

        public QueryFieldCollection? UpdateFieldValues { get; set; }

        #endregion

        #region Insert Statement.

        /// <summary>
        /// Query that needs to be executed to get the insert values for a "insert into, select from" statement.
        /// </summary>
        public PreparedQuery? InsertSelectQuery { get; set; }

        /// <summary>
        /// Values that are used when executing a "insert into, values" statement.
        /// </summary>
        public List<QueryFieldCollection>? InsertFieldValues { get; set; }

        /// <summary>
        /// The field names that are to be used when inserting values from InsertFieldValues or InsertSelectQuery.
        /// </summary>
        public List<string> InsertFieldNames { get; set; } = new();

        #endregion

        public List<KbNameValuePair<string, string>> VariableValues { get; set; } = new();

        public PreparedQuery(QueryBatch queryBatch, QueryType queryType)
        {
            QueryType = queryType;
            Batch = queryBatch;
            Conditions = new(queryBatch);
            SelectFields = new(queryBatch);
            GroupFields = new(queryBatch);
        }

        public T Attribute<T>(QueryAttribute attribute, T defaultValue)
        {
            if (Attributes.TryGetValue(attribute, out object? value))
            {
                return (T)value;
            }
            return defaultValue;
        }

        public T Attribute<T>(QueryAttribute attribute)
        {
            return (T)Attributes[attribute];
        }

        public void AddAttribute(QueryAttribute key, object value)
        {
            if (!Attributes.TryAdd(key, value))
            {
                Attributes[key] = value;
            }
        }

        public void AddAttributes(Dictionary<QueryAttribute, object> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (!Attributes.TryAdd(attribute.Key, attribute.Value))
                {
                    Attributes[attribute.Key] = attribute.Value;
                }
            }
        }
    }
}
