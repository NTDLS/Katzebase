using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Schemas;

namespace NTDLS.Katzebase.Engine.Threading.PoolingParameters
{
    /// <summary>
    /// Thread parameters for a lookup operations. Shared across all threads in a single lookup operation.
    /// </summary>
    internal class RemoveDocumentsFromIndexThreadOperation
    {
        public Transaction Transaction { get; set; }
        public PhysicalIndex PhysicalIndex { get; set; }
        public PhysicalSchema PhysicalSchema { get; set; }
        public IEnumerable<DocumentPointer> DocumentPointers { get; set; }

        public RemoveDocumentsFromIndexThreadOperation(Transaction transaction,
            PhysicalIndex physicalIndex, PhysicalSchema physicalSchema, IEnumerable<DocumentPointer> documentPointers)
        {
            Transaction = transaction;
            PhysicalIndex = physicalIndex;
            PhysicalSchema = physicalSchema;
            DocumentPointers = documentPointers;
        }
    }
}
