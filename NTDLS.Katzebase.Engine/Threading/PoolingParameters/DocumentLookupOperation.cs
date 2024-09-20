using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping;

namespace NTDLS.Katzebase.Engine.Threading.PoolingParameters
{

    /// <summary>
    /// Thread parameters for a lookup operations. Shared across all threads in a single operation.
    /// </summary>
    internal class DocumentLookupOperation
    {
        /// <summary>
        /// Contains the list of field values for the grouping fields, and the need-to-be aggregated values for fields
        /// that are needed to collapse aggregation functions. The key is the concatenated values from the grouping fields.
        /// </summary>
        public Dictionary<string, GroupRowCollection> GroupRows { get; set; } = new();

        public string[]? GatherDocumentsIdsForSchemaPrefixes { get; set; } = null;
        public SchemaIntersectionRowCollection ResultingRows { get; set; } = new();
        public List<SchemaIntersectionRowDocumentIdentifier> RowDocumentIdentifiers { get; set; } = new();
        public QuerySchemaMap SchemaMap { get; private set; }
        public EngineCore Core { get; private set; }
        public Transaction Transaction { get; private set; }
        public PreparedQuery Query { get; private set; }

        public DocumentLookupOperation(EngineCore core, Transaction transaction,
            QuerySchemaMap schemaMap, PreparedQuery query, string[]? getDocumentsIdsForSchemaPrefixes)
        {
            GatherDocumentsIdsForSchemaPrefixes = getDocumentsIdsForSchemaPrefixes;
            Core = core;
            Transaction = transaction;
            SchemaMap = schemaMap;
            Query = query;
        }

        /// <summary>
        /// Thread parameters for a lookup operations. Used by a single thread.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="documentPointer"></param>
        internal class Instance(DocumentLookupOperation operation, DocumentPointer documentPointer)
        {
            public Semaphore.OptimisticCriticalResource<Dictionary<string, NCalc.Expression>> ExpressionCache { get; set; } = new();
            public DocumentLookupOperation Operation { get; set; } = operation;
            public DocumentPointer DocumentPointer { get; set; } = documentPointer;
        }
    }
}
