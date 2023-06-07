using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;
using static Katzebase.Engine.Documents.DocumentManager;

namespace Katzebase.Engine.Documents.Threading
{
    internal class DocumentLookupThreadParam
    {
        public DocumentLookupResults Results { get; set; }
        public Transaction Transaction { get; set; }
        public QuerySchemaMap SchemaMap { get; set; }
        public PreparedQuery Query { get; set; }
        public ConditionLookupOptimization LookupOptimization { get; set; }
        public int ThreadSlotNumber { get; private set; }
        public List<DocumentLookupThreadSlot> ThreadSlots { get; set; }
        public PerformanceTrace? PT { get; private set; }

        public DocumentLookupThreadParam(PerformanceTrace? pt, Transaction transaction, QuerySchemaMap schemaMap, PreparedQuery query,
            ConditionLookupOptimization lookupOptimization, List<DocumentLookupThreadSlot> threadSlots, int threadSlotNumber, DocumentLookupResults results)
        {
            ThreadSlotNumber = threadSlotNumber;
            Transaction = transaction;
            SchemaMap = schemaMap;
            Query = query;
            LookupOptimization = lookupOptimization;
            ThreadSlots = threadSlots;
            Results = results;
            PT = pt;
        }
    }
}
