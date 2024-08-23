using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Query.Constraints;
using NTDLS.Katzebase.Engine.Schemas;

namespace NTDLS.Katzebase.Engine.Threading.PoolingParameters
{
    /// <summary>
    /// Thread parameters for a index operations. Shared across all threads in a single lookup operation.
    /// </summary>
    class MatchSchemaDocumentsByWhereClauseOperation
    {
        public Dictionary<uint, DocumentPointer> ThreadResults = new();
        public Transaction Transaction { get; set; }
        public IndexingConditionLookup Lookup { get; set; }
        public PhysicalSchema PhysicalSchema { get; set; }
        public string WorkingSchemaPrefix { get; set; }
        public Condition Condition { get; set; }

        public MatchSchemaDocumentsByWhereClauseOperation(Transaction transaction, IndexingConditionLookup lookup,
            PhysicalSchema physicalSchema, string workingSchemaPrefix, Condition condition)
        {
            Transaction = transaction;
            Lookup = lookup;
            PhysicalSchema = physicalSchema;
            WorkingSchemaPrefix = workingSchemaPrefix;
            Condition = condition;
        }

        public class Parameter
        {
            public MatchSchemaDocumentsByWhereClauseOperation Operation { get; set; }
            public uint IndexPartition { get; set; }

            public Parameter(MatchSchemaDocumentsByWhereClauseOperation operation, uint indexPartition)
            {
                Operation = operation;
                IndexPartition = indexPartition;
            }
        }
    }
}
