using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Parsers.Query.Conditions;
using NTDLS.Katzebase.Parsers.Query.Fields;
using System.Diagnostics.CodeAnalysis;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    /// <summary>
    /// Contains a parsed query via StaticParserQuery.Parse();
    /// </summary>
    public class Query(QueryBatch queryBatch, QueryType queryType, int? fileLine)
    {
        public enum Attribute
        {
            VariablePlaceholder,
            Expression,
            IsAdministrator,
            Username,
            Password,
            PasswordHash,
            RoleName,
            IndexName,
            IsUnique,
            ProcessId,
            IncludePhysicalPages,
            TargetSchemaName,
            TargetSchemaAlias,
            Schema,
            ObjectName,
            Parameters,
            Batches,
            Partitions,
            PageSize,
        }

        /// <summary>
        /// The line that the query started on.
        /// </summary>
        public int? ScriptLine { get; set; } = fileLine;

        public List<KbNameValuePair<string, string>> VariableValues { get; set; } = new();

        /// <summary>
        /// Contains the hash of the whole query text with all constants and variables removed.
        /// </summary>
        public string? Hash { get; set; }
        public QueryBatch Batch { get; private set; } = queryBatch;
        public List<QuerySchema> Schemas { get; private set; } = new();
        public QueryType QueryType { get; set; } = queryType;
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
        public ConditionCollection Conditions { get; set; } = new(queryBatch);

        #endregion

        #region Select Statement.

        public SelectFieldCollection SelectFields { get; set; } = new(queryBatch);
        public GroupByFieldCollection GroupBy { get; set; } = new(queryBatch);
        public OrderByFieldCollection OrderBy { get; set; } = new(queryBatch);

        #endregion

        #region Update Statement.

        public List<string> UpdateFieldNames { get; set; } = new();

        public QueryFieldCollection UpdateFieldValues { get; set; } = new(queryBatch);

        #endregion

        #region Insert Statement.

        /// <summary>
        /// Query that needs to be executed to get the insert values for a "insert into, select from" statement.
        /// </summary>
        public Query? InsertSelectQuery { get; set; }

        /// <summary>
        /// Values that are used when executing a "insert into, values" statement.
        /// </summary>
        public List<QueryFieldCollection>? InsertFieldValues { get; set; }

        /// <summary>
        /// The field names that are to be used when inserting values from InsertFieldValues or InsertSelectQuery.
        /// </summary>
        public List<string> InsertFieldNames { get; set; } = new();

        #endregion

        #region Add/Get Attributes.

        private readonly KbInsensitiveDictionary<QueryAttribute> _attributes = new();
        public IReadOnlyDictionary<string, QueryAttribute> Attributes => _attributes;

        public bool IsAttributeSet(Attribute attribute)
            => _attributes.TryGetValue(attribute.ToString(), out var _);

        public bool IsAttributeSet(string attribute)
            => _attributes.TryGetValue(attribute, out var _);

        public bool TryGetAttribute<T>(string attribute, out T outValue, T defaultValue)
        {
            if (_attributes.TryGetValue(attribute, out var option))
            {
                outValue = (T)option.Value;
                return true;
            }
            outValue = defaultValue;
            return false;
        }

        public bool TryGetAttribute<T>(string attribute, [NotNullWhen(true)] out T? outValue)
        {
            if (_attributes.TryGetValue(attribute, out var option))
            {
                outValue = (T)option.Value;
                return true;
            }
            outValue = default;
            return false;
        }

        public T GetAttribute<T>(string attribute, T defaultValue)
            => _attributes.TryGetValue(attribute, out var option) ? (T)option.Value : defaultValue;

        public T GetAttribute<T>(string attribute)
            => (T)_attributes[attribute].Value;

        public void AddAttribute<T>(string key, T value) where T : notnull
            => _attributes[key] = new QueryAttribute(key, value, value.GetType());

        public bool TryGetAttribute<T>(Attribute attribute, out T outValue, T defaultValue)
            => TryGetAttribute(attribute.ToString(), out outValue, defaultValue);

        public bool TryGetAttribute<T>(Attribute attribute, [NotNullWhen(true)] out T? outValue)
            => TryGetAttribute(attribute.ToString(), out outValue);

        public T GetAttribute<T>(Attribute attribute, T defaultValue)
            => GetAttribute(attribute.ToString(), defaultValue);

        public T GetAttribute<T>(Attribute attribute)
            => GetAttribute<T>(attribute.ToString());

        public void AddAttribute<T>(Attribute key, T value) where T : notnull
            => AddAttribute(key.ToString(), value);

        public void AddAttributes(KbInsensitiveDictionary<QueryAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                _attributes.Add(attribute.Key, attribute.Value);
            }
        }

        #endregion
    }
}
