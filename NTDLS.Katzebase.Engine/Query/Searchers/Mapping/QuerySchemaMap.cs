﻿using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Documents;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;
using Katzebase.Types;

namespace Katzebase.Engine.Query.Searchers.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// The key to the dictionary is the schema alias (typically referenced by Condition.Prefix).
    /// </summary>
    internal class QuerySchemaMap : KbInsensitiveDictionary<QuerySchemaMapItem>
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
        /// <param name="documentCatalog">The document catalog contained in the associated schema.</param>
        /// <param name="conditions">The conditons used to join this schema mapping to the one before it.</param>
        public void Add(string prefix, PhysicalSchema physicalSchema, PhysicalDocumentPageCatalog documentCatalog, Conditions? conditions)
        {
            Add(prefix, new QuerySchemaMapItem(core, Transaction, this, physicalSchema, documentCatalog, conditions, prefix));
        }

        public int TotalDocumentCount()
        {
            return this.Sum(o => o.Value.DocumentPageCatalog.TotalDocumentCount());
        }
    }
}