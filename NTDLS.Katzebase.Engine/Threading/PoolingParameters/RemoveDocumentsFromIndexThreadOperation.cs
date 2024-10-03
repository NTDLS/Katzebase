using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Schemas;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Threading.PoolingParameters
{
    /// <summary>
    /// Thread parameters for index row removal operations. Shared across all threads in a single operation.
    /// </summary>
    internal class RemoveDocumentsFromIndexThreadOperation<TData> where TData:IStringable
    {
        public Transaction<TData> Transaction { get; set; }
        public PhysicalIndex<TData> PhysicalIndex { get; set; }
        public PhysicalSchema<TData> PhysicalSchema { get; set; }
        public IEnumerable<DocumentPointer<TData>> DocumentPointers { get; set; }

        public RemoveDocumentsFromIndexThreadOperation(Transaction<TData> transaction,
            PhysicalIndex<TData> physicalIndex, PhysicalSchema<TData> physicalSchema, IEnumerable<DocumentPointer<TData>> documentPointers)
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
            internal RemoveDocumentsFromIndexThreadOperation<TData> Operation { get; set; }
            internal int IndexPartition { get; set; }

            internal Instance(RemoveDocumentsFromIndexThreadOperation<TData> operation, int indexPartition)
            {
                Operation = operation;
                IndexPartition = indexPartition;
            }
        }
    }
}
