﻿using NTDLS.Katzebase.Engine.Atomicity;
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
                var ptOptimization = transaction.Instrumentation.CreateToken(PerformanceCounter.Optimization);
                Optimization = IndexingConditionOptimization.BuildTree(core, transaction, schemaMap.Query, physicalSchema, conditions, prefix);
                ptOptimization?.StopAndAccumulate();
            }
        }
    }
}
