using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Parsers.Conditions;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.PersistentTypes.Schema;
using static NTDLS.Katzebase.Engine.Instrumentation.InstrumentationTracker;
using static NTDLS.Katzebase.Parsers.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// </summary>
    internal class QuerySchemaOptimizationMapItem
    {
        public string SchemaPrefix { get; private set; }
        public PhysicalSchema PhysicalSchema { get; private set; }
        public PhysicalDocumentPageCatalog DocumentPageCatalog { get; private set; }
        public ConditionCollection? Conditions { get; private set; }
        public IndexingConditionOptimization? Optimization { get; private set; }
        public QuerySchemaUsageType SchemaUsageType { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key">The alias of the schema</param>
        /// <param name="physicalSchema">The associated schema meta-data.</param>
        /// <param name="documentCatalog">The document catalog contained in the associated schema.</param>
        /// <param name="conditions">The conditions used to join this schema mapping to the one before it.</param>
        public QuerySchemaOptimizationMapItem(EngineCore core, Transaction transaction, QuerySchemaOptimizationMap schemaMap, PhysicalSchema physicalSchema,
            QuerySchemaUsageType schemaUsageType, PhysicalDocumentPageCatalog documentPageCatalog, ConditionCollection? conditions, string schemaPrefix)
        {
            SchemaPrefix = schemaPrefix;
            PhysicalSchema = physicalSchema;
            SchemaUsageType = schemaUsageType;
            DocumentPageCatalog = documentPageCatalog;
            Conditions = conditions;

            if (conditions != null)
            {
                var ptOptimization = transaction.Instrumentation.CreateToken(PerformanceCounter.Optimization);
                Optimization = IndexingConditionOptimization.SelectUsableIndexes(core, transaction, schemaMap.Query, physicalSchema, conditions, schemaPrefix);
                ptOptimization?.StopAndAccumulate();
            }
        }
    }
}
