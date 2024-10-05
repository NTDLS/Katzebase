using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    /// <summary>
    /// Contains a parsed query via StaticQueryParser.PrepareQuery();
    /// </summary>
    public class PreparedQuery
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

        private readonly Dictionary<QueryAttribute, object> _attributes = new();
        public IReadOnlyDictionary<QueryAttribute, object> Attributes => _attributes;

        /// <summary>
        /// The line that the query started on.
        /// </summary>
        public int? ScriptLine { get; set; }

        /// <summary>
        /// Contains the hash of the whole query text with all constants and variables removed.
        /// </summary>
        public string? Hash { get; set; }
        public QueryBatch Batch { get; private set; }
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
        public List<string>? DynamicSchemaFieldFilter { get; set; } = null;
        public int RowLimit { get; set; }
        public int RowOffset { get; set; }
        public ConditionCollection Conditions { get; set; }

        #endregion

        #region Select Statement.

        public SelectFieldCollection SelectFields { get; set; }
        public GroupByFieldCollection GroupBy { get; set; }
        public OrderByFieldCollection OrderBy { get; set; }

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

        public PreparedQuery(QueryBatch queryBatch, QueryType queryType, int? fileLine)
        {
            QueryType = queryType;
            Batch = queryBatch;
            ScriptLine = fileLine;

            Conditions = new(queryBatch);
            SelectFields = new(queryBatch);
            GroupBy = new(queryBatch);
            OrderBy = new(queryBatch);
        }

        public T TryGetAttribute<T>(QueryAttribute attribute, T defaultValue)
        {
            if (_attributes.TryGetValue(attribute, out object? value))
            {
                return (T)value;
            }
            return defaultValue;
        }

        public T GetAttribute<T>(QueryAttribute attribute)
        {
            return (T)_attributes[attribute];
        }

        public void AddAttribute(QueryAttribute key, object value)
        {
            if (!_attributes.TryAdd(key, value))
            {
                _attributes[key] = value;
            }
        }

        public void AddAttributes(Dictionary<QueryAttribute, object> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (!_attributes.TryAdd(attribute.Key, attribute.Value))
                {
                    _attributes[attribute.Key] = attribute.Value;
                }
            }
        }
    }
}
