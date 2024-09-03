using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Schemas;

namespace NTDLS.Katzebase.Engine.Threading.PoolingParameters
{
    /// <summary>
    /// Thread parameters for index row removal operations. Shared across all threads in a single operation.
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

        /// <summary>
        /// Thread parameters for a index row removal operation. Used by a single thread.
        /// </summary>
        internal class Instance
        {
            internal RemoveDocumentsFromIndexThreadOperation Operation { get; set; }
            internal int IndexPartition { get; set; }

            internal Instance(RemoveDocumentsFromIndexThreadOperation operation, int indexPartition)
            {
                Operation = operation;
                IndexPartition = indexPartition;
            }
        }
    }
}
