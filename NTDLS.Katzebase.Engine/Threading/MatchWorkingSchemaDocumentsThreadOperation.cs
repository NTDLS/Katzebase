using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Indexes.Matching;
using NTDLS.Katzebase.Engine.Query.Constraints;
using NTDLS.Katzebase.Engine.Schemas;

namespace NTDLS.Katzebase.Engine.Threading
{
    /// <summary>
    /// Thread parameters for a index operations. Shared across all threads in a single lookup operation.
    /// </summary>
    internal class MatchWorkingSchemaDocumentsThreadOperation
    {
        public Transaction Transaction { get; set; }
        public PhysicalIndex PhysicalIndex { get; set; }
        public PhysicalSchema PhysicalSchema { get; set; }
        public IndexSelection IndexSelection { get; set; }
        public ConditionSubset ConditionSubset { get; set; }
        public string WorkingSchemaPrefix { get; set; }
        public Dictionary<uint, DocumentPointer> Results { get; set; } = new();

        public MatchWorkingSchemaDocumentsThreadOperation(Transaction transaction, PhysicalIndex physicalIndex,
            PhysicalSchema physicalSchema, IndexSelection indexSelection, ConditionSubset conditionSubset, string workingSchemaPrefix)
        {
            Transaction = transaction;
            PhysicalIndex = physicalIndex;
            PhysicalSchema = physicalSchema;
            IndexSelection = indexSelection;
            ConditionSubset = conditionSubset;
            WorkingSchemaPrefix = workingSchemaPrefix;
        }
    }
}
