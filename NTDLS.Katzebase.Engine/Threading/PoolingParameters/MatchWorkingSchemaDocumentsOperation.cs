using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Query.Constraints;
using NTDLS.Katzebase.Engine.Schemas;

namespace NTDLS.Katzebase.Engine.Threading.PoolingParameters
{
    /// <summary>
    /// Thread parameters for a index operations. Shared across all threads in a single lookup operation.
    /// </summary>
    internal class MatchWorkingSchemaDocumentsOperation
    {
        public Transaction Transaction { get; set; }
        public PhysicalIndex PhysicalIndex { get; set; }
        public PhysicalSchema PhysicalSchema { get; set; }
        public IndexSelection IndexSelection { get; set; }
        public SubCondition GivenSubCondition { get; set; }
        public string WorkingSchemaPrefix { get; set; }
        public Dictionary<uint, DocumentPointer> Results { get; set; } = new();

        public MatchWorkingSchemaDocumentsOperation(Transaction transaction, PhysicalIndex physicalIndex,
            PhysicalSchema physicalSchema, IndexSelection indexSelection,
            SubCondition givenSubCondition, string workingSchemaPrefix)
        {
            Transaction = transaction;
            PhysicalIndex = physicalIndex;
            PhysicalSchema = physicalSchema;
            IndexSelection = indexSelection;
            GivenSubCondition = givenSubCondition;
            WorkingSchemaPrefix = workingSchemaPrefix;
        }

        /// <summary>
        /// Thread parameters for a index operations. Used by a single thread.
        /// </summary>
        internal class MatchWorkingSchemaDocumentsInstance(MatchWorkingSchemaDocumentsOperation operation, int indexPartition)
        {
            public MatchWorkingSchemaDocumentsOperation Operation { get; set; } = operation;
            public int IndexPartition { get; set; } = indexPartition;
        }
    }
}
