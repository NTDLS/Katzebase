using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Mapping
{
    /// <summary>
    /// This class maps the schema and documents to a query supplied schema alias.
    /// The key to the dictionary is the schema alias (typically referenced by Condition.Prefix).
    /// </summary>
    internal class QuerySchemaOptimizationMap(EngineCore core, Transaction transaction, PreparedQuery query)
        : KbInsensitiveDictionary<QuerySchemaOptimizationMapItem>
    {
        private readonly EngineCore _core = core;
        public Transaction Transaction { get; private set; } = transaction;
        public PreparedQuery Query { get; private set; } = query;

        public int TotalDocumentCount()
        {
            return this.Sum(o => o.Value.DocumentPageCatalog.TotalDocumentCount());
        }
    }
}
