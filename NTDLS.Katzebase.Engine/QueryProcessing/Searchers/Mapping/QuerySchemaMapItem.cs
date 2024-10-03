using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Schemas;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.PersistentTypes.Document;
using static NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping.QuerySchemaMap;
using static NTDLS.Katzebase.Parsers.Query.SupportingTypes.QuerySchema;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// </summary>
    internal class QuerySchemaMapItem
    {
        public string Prefix { get; private set; }
        public PhysicalSchema PhysicalSchema { get; private set; }
        public PhysicalDocumentPageCatalog DocumentPageCatalog { get; private set; }
        public ConditionCollection? Conditions { get; private set; }
        public IndexingConditionOptimization? Optimization { get; private set; }
        public QuerySchemaUsageType SchemaUsageType { get; private set; }

        public QuerySchemaMapItem(EngineCore core, Transaction transaction, QuerySchemaMap schemaMap, PhysicalSchema physicalSchema,
            QuerySchemaUsageType schemaUsageType, PhysicalDocumentPageCatalog documentPageCatalog, ConditionCollection? conditions, string prefix)
        {
            Prefix = prefix;
            PhysicalSchema = physicalSchema;
            SchemaUsageType = schemaUsageType;
            DocumentPageCatalog = documentPageCatalog;
            Conditions = conditions;

            if (conditions != null)
            {
                Optimization = IndexingConditionOptimization.BuildTree(core, transaction, schemaMap.Query, physicalSchema, conditions, prefix);
            }
        }
    }
}
