using Katzebase.Engine.Documents.Query.SingleSchema;
using Katzebase.Engine.Query;
using Katzebase.Engine.Query.Condition;
using Katzebase.Engine.Schemas;
using Katzebase.Engine.Trace;
using Katzebase.Engine.Transactions;

namespace Katzebase.Engine.Documents.Query.SingleSchema.Threading
{
    internal class SSQDocumentLookupThreadParam
    {
        public SSQDocumentLookupResults Results { get; set; }
        public Transaction Transaction { get; set; }
        public PersistSchema SchemaMeta { get; set; }
        public PreparedQuery Query { get; set; }
        public ConditionLookupOptimization LookupOptimization { get; set; }
        public int ThreadSlotNumber { get; private set; }
        public List<SSQDocumentLookupThreadSlot> ThreadSlots { get; set; }
        public PerformanceTrace? PT { get; private set; }
        public Core Core { get; set; }

        public SSQDocumentLookupThreadParam(Core core, PerformanceTrace? pt, Transaction transaction, PersistSchema schemaMeta, PreparedQuery query,
            ConditionLookupOptimization lookupOptimization, List<SSQDocumentLookupThreadSlot> threadSlots, int threadSlotNumber, SSQDocumentLookupResults results)
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
