using Katzebase.Engine.Documents;
using Katzebase.Engine.Query.Constraints;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Transactions;

namespace Katzebase.Engine.Query.Searchers.MultiSchema.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// </summary>
    internal class MSQQuerySchemaMapItem
    {
        public PersistSchema PhysicalSchema { get; set; }
        public PersistDocumentPageCatalog DocuemntCatalog { get; set; }
        public Conditions? Conditions { get; set; }

        public ConditionLookupOptimization? Optimization { get; set; }

        public MSQQuerySchemaMapItem(Core core, Transaction transaction, PersistSchema physicalSchema, PersistDocumentPageCatalog docuemntCatalog, Conditions? conditions)
        {
            PhysicalSchema = physicalSchema;
            DocuemntCatalog = docuemntCatalog;
            Conditions = conditions;

            if (conditions != null)
            {
                Optimization = ConditionLookupOptimization.Build(core, transaction, physicalSchema, conditions);
            }
        }
    }
}
