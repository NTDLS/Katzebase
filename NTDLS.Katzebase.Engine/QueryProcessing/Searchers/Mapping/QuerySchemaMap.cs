using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.PersistentTypes.Schema;
using static NTDLS.Katzebase.Parsers.Query.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// The key to the dictionary is the schema alias (typically referenced by Condition.Prefix).
    /// </summary>
    internal class QuerySchemaMap(EngineCore core, Transaction transaction, PreparedQuery query) : KbInsensitiveDictionary<QuerySchemaMapItem>
    {
        private readonly EngineCore _core = core;
        public Transaction Transaction { get; private set; } = transaction;
        public PreparedQuery Query { get; private set; } = query;

        /// <summary>
        /// Adds a mapping to the schema mapping collection.
        /// </summary>
        /// <param name="key">The alias of the schema</param>
        /// <param name="physicalSchema">The associated schema meta-data.</param>
        /// <param name="documentCatalog">The document catalog contained in the associated schema.</param>
        /// <param name="conditions">The conditions used to join this schema mapping to the one before it.</param>
        public void Add(string prefix, PhysicalSchema physicalSchema, QuerySchemaUsageType schemaUsageType, PhysicalDocumentPageCatalog documentCatalog, ConditionCollection? conditions)
        {
            prefix = prefix.ToLowerInvariant();
            Add(prefix, new QuerySchemaMapItem(_core, Transaction, this, physicalSchema, schemaUsageType, documentCatalog, conditions, prefix));
        }

        public int TotalDocumentCount()
        {
            return this.Sum(o => o.Value.DocumentPageCatalog.TotalDocumentCount());
        }
    }
}
