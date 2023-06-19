using Katzebase.Engine.Atomicity;
using Katzebase.Engine.Documents;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Query.Searchers.MultiSchema;
using Katzebase.Engine.Schemas;

namespace Katzebase.Engine.Query.Searchers.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// </summary>
    internal class QuerySchemaMapItem
    {
        public string Prefix { get; set; }
        public PhysicalSchema PhysicalSchema { get; set; }
        public PhysicalDocumentPageCatalog DocumentPageCatalog { get; set; }
        public Conditions? Conditions { get; set; }

        public ConditionLookupOptimization? Optimization { get; set; }

        public QuerySchemaMapItem(Core core, Transaction transaction, QuerySchemaMap schemaMap, PhysicalSchema physicalSchema,
            PhysicalDocumentPageCatalog documentPageCatalog, Conditions? conditions, string prefix)
        {
            Prefix = prefix;
            PhysicalSchema = physicalSchema;
            DocumentPageCatalog = documentPageCatalog;
            Conditions = conditions;

            if (conditions != null)
            {
                Optimization = ConditionLookupOptimization.Build(core, transaction, physicalSchema, conditions, prefix);
            }
        }
    }
}
