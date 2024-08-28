using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Query;
using NTDLS.Katzebase.Engine.Query.Searchers.Intersection;
using NTDLS.Katzebase.Engine.Query.Searchers.Mapping;

namespace NTDLS.Katzebase.Engine.Threading.PoolingParameters
{
    /// <summary>
    /// Thread parameters for a lookup operations. Shared across all threads in a single lookup operation.
    /// </summary>
    internal class DocumentLookupOperation
    {
        public string? GatherDocumentPointersForSchemaPrefix { get; set; } = null;
        public SchemaIntersectionRowCollection Results { get; set; } = new();
        public List<DocumentPointer> DocumentPointers { get; set; } = new();
        public QuerySchemaMap SchemaMap { get; private set; }
        public EngineCore Core { get; private set; }
        public Transaction Transaction { get; private set; }
        public PreparedQuery Query { get; private set; }

        public DocumentLookupOperation(EngineCore core, Transaction transaction,
            QuerySchemaMap schemaMap, PreparedQuery query, string? gatherDocumentPointersForSchemaPrefix)
        {
            GatherDocumentPointersForSchemaPrefix = gatherDocumentPointersForSchemaPrefix;
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
        internal class Parameter(DocumentLookupOperation operation, DocumentPointer documentPointer)
        {
            public DocumentLookupOperation Operation { get; set; } = operation;
            public DocumentPointer DocumentPointer { get; set; } = documentPointer;
            public Dictionary<string, NCalc.Expression> ExpressionCache { get; set; } = new();
        }
    }
}
