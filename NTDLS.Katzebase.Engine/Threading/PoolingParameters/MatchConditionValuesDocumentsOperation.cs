using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Query.Constraints;
using NTDLS.Katzebase.Engine.Schemas;

namespace NTDLS.Katzebase.Engine.Threading.PoolingParameters
{
    /// <summary>
    /// Thread parameters for a lookup operations. Shared across all threads in a single lookup operation.
    /// </summary>
    internal class MatchConditionValuesDocumentsOperation
    {
        public Transaction Transaction { get; set; }
        public PhysicalIndex PhysicalIndex { get; set; }
        public PhysicalSchema PhysicalSchema { get; set; }
        public IndexSelection IndexSelection { get; set; }
        public SubCondition ConditionSubCondition { get; set; }
        public KbInsensitiveDictionary<string> ConditionValues { get; set; }
        public Dictionary<uint, DocumentPointer> Results { get; set; } = new();

        public MatchConditionValuesDocumentsOperation(Transaction transaction,
            PhysicalIndex physicalIndex, PhysicalSchema physicalSchema, IndexSelection indexSelection,
            SubCondition conditionSubCondition, KbInsensitiveDictionary<string> conditionValues)
        {
            Transaction = transaction;
            PhysicalIndex = physicalIndex;
            PhysicalSchema = physicalSchema;
            IndexSelection = indexSelection;
            ConditionSubCondition = conditionSubCondition;
            ConditionValues = conditionValues;
        }

        /// <summary>
        /// Thread parameters for a lookup operations. Used by a single thread.
        /// </summary>
        internal class MatchConditionValuesDocumentsInstance(MatchConditionValuesDocumentsOperation operation, int indexPartition)
        {
            public MatchConditionValuesDocumentsOperation Operation { get; set; } = operation;
            public int IndexPartition { get; set; } = indexPartition;
        }
    }
}
