using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Query.Searchers.Intersection;
using NTDLS.Katzebase.Engine.Query.Searchers.Mapping;

namespace NTDLS.Katzebase.Engine.Query.Searchers
{
    /// <summary>
    /// Thread parameters for a lookup operation. Shared across all threads.
    /// </summary>
    internal class LookupThreadOperation
    {
        public string? GatherDocumentPointersForSchemaPrefix { get; set; } = null;
        public SchemaIntersectionRowCollection Results { get; set; } = new();
        public List<DocumentPointer> DocumentPointers { get; set; } = new();
        public QuerySchemaMap SchemaMap { get; private set; }
        public EngineCore Core { get; private set; }
        public Transaction Transaction { get; private set; }
        public PreparedQuery Query { get; private set; }

        public LookupThreadOperation(EngineCore core, Transaction transaction, QuerySchemaMap schemaMap, PreparedQuery query, string? gatherDocumentPointersForSchemaPrefix)
        {
            GatherDocumentPointersForSchemaPrefix = gatherDocumentPointersForSchemaPrefix;
            Core = core;
            Transaction = transaction;
            SchemaMap = schemaMap;
            Query = query;
        }
    }
}
