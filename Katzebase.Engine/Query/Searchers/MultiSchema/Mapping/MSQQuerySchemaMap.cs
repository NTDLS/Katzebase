using Katzebase.Engine.Documents;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Transactions;

namespace Katzebase.Engine.Query.Searchers.MultiSchema.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// The key to the dictonary is the schema alias (typically referenced by Condition.Prefix).
    /// </summary>
    internal class MSQQuerySchemaMap : Dictionary<string, MSQQuerySchemaMapItem>
    {
        private Core core;
        public Transaction Transaction { get; private set; }

        public MSQQuerySchemaMap(Core core, Transaction transaction)
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
        public void Add(string key, PhysicalSchema physicalSchema, PhysicalDocumentPageCatalog docuemntCatalog, Conditions? conditions)
        {
            Add(key, new MSQQuerySchemaMapItem(core, Transaction, physicalSchema, docuemntCatalog, conditions));
        }

        public int TotalDocumentCount()
        {
            return this.Sum(o => o.Value.DocumentPageCatalog.TotalDocumentCount());
        }

    }
}
