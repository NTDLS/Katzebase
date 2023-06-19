using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Documents;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;

namespace Katzebase.Engine.Query.Searchers.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// The key to the dictonary is the schema alias (typically referenced by Condition.Prefix).
    /// </summary>
    internal class QuerySchemaMap : Dictionary<string, QuerySchemaMapItem>
    {
        private readonly Core core;
        public Transaction Transaction { get; private set; }

        public QuerySchemaMap(Core core, Transaction transaction)
        {
            this.core = core;
            Transaction = transaction;
        }

        /// <summary>
        /// Adds a mapping to the schema mapping collection.
        /// </summary>
        /// <param name="key">The alias of the schema</param>
        /// <param name="physicalSchema">The associated schema meta-data.</param>
        /// <param name="docuemntCatalog">The document catalog contained in the associated schema.</param>
        /// <param name="conditions">The conditons used to join this schema mapping to the one before it.</param>
        public void Add(string prefix, PhysicalSchema physicalSchema, PhysicalDocumentPageCatalog docuemntCatalog, Conditions? conditions)
        {
            Add(prefix, new QuerySchemaMapItem(core, Transaction, this, physicalSchema, docuemntCatalog, conditions, prefix));
        }

        public int TotalDocumentCount()
        {
            return this.Sum(o => o.Value.DocumentPageCatalog.TotalDocumentCount());
        }
    }
}
