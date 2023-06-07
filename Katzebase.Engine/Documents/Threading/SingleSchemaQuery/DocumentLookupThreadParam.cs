using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;

namespace Katzebase.Engine.Documents.Threading.SingleSchemaQuery
{
    internal class DocumentLookupThreadParam
    {
        public DocumentLookupResults Results { get; set; }
        public Transaction Transaction { get; set; }
        public PersistSchema SchemaMeta { get; set; }
        public PreparedQuery Query { get; set; }
        public ConditionLookupOptimization LookupOptimization { get; set; }
        public int ThreadSlotNumber { get; private set; }
        public List<DocumentLookupThreadSlot> ThreadSlots { get; set; }
        public PerformanceTrace? PT { get; private set; }
        public Core Core { get; set; }

        public DocumentLookupThreadParam(Core core, PerformanceTrace? pt, Transaction transaction, PersistSchema schemaMeta, PreparedQuery query,
            ConditionLookupOptimization lookupOptimization, List<DocumentLookupThreadSlot> threadSlots, int threadSlotNumber, DocumentLookupResults results)
        {
            Core = core;
            ThreadSlotNumber = threadSlotNumber;
            Transaction = transaction;
            SchemaMeta = schemaMeta;
            Query = query;
            LookupOptimization = lookupOptimization;
            ThreadSlots = threadSlots;
            Results = results;
            PT = pt;
        }
    }
}
