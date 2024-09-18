using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Engine.Schemas;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// The key to the dictionary is the schema alias (typically referenced by Condition.Prefix).
    /// </summary>
    internal class QuerySchemaMap : KbInsensitiveDictionary<QuerySchemaMapItem>
    {
        private readonly EngineCore _core;
        public Transaction Transaction { get; private set; }
        public PreparedQuery Query { get; private set; }

        public QuerySchemaMap(EngineCore core, Transaction transaction, PreparedQuery query)
        {
            _core = core;
            Query = query;
            Transaction = transaction;
        }

        /// <summary>
        /// Adds a mapping to the schema mapping collection.
        /// </summary>
        /// <param name="key">The alias of the schema</param>
        /// <param name="physicalSchema">The associated schema meta-data.</param>
        /// <param name="documentCatalog">The document catalog contained in the associated schema.</param>
        /// <param name="conditions">The conditions used to join this schema mapping to the one before it.</param>
        public void Add(string prefix, PhysicalSchema physicalSchema, PhysicalDocumentPageCatalog documentCatalog, ConditionCollection? conditions)
        {
            Add(prefix, new QuerySchemaMapItem(_core, Transaction, this, physicalSchema, documentCatalog, conditions, prefix));
        }

        public int TotalDocumentCount()
        {
            return this.Sum(o => o.Value.DocumentPageCatalog.TotalDocumentCount());
        }
    }
}
