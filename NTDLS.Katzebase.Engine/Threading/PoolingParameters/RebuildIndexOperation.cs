using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Schemas;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Threading.PoolingParameters
{
    /// <summary>
    /// Thread parameters for a index rebuild. Shared across all threads in a single operation.
    /// </summary>
    internal class RebuildIndexOperation<TData> where TData : IStringable
    {
        public Transaction<TData> Transaction { get; set; }
        public PhysicalSchema<TData> PhysicalSchema { get; set; }
        public PhysicalIndex<TData> PhysicalIndex { get; set; }
        public Dictionary<uint, PhysicalIndexPages<TData>> PhysicalIndexPageMap { get; set; }
        public object[] SyncObjects { get; private set; }

        public RebuildIndexOperation(Transaction<TData> transaction, PhysicalSchema<TData> physicalSchema,
            Dictionary<uint, PhysicalIndexPages<TData>> physicalIndexPageMap, PhysicalIndex<TData> physicalIndex, uint indexPartitions)
        {
            SyncObjects = new object[indexPartitions];

            for (uint indexPartition = 0; indexPartition < indexPartitions; indexPartition++)
            {
                SyncObjects[indexPartition] = new object();
            }

            Transaction = transaction;
            PhysicalSchema = physicalSchema;
            PhysicalIndex = physicalIndex;
            PhysicalIndexPageMap = physicalIndexPageMap;
        }

        /// <summary>
        /// Thread parameters for a index rebuild operation. Used by a single thread.
        /// </summary>
        internal class Instance(RebuildIndexOperation<TData> operation, int pageNumber)
        {
            public RebuildIndexOperation<TData> Operation { get; set; } = operation;
            public int PageNumber { get; set; } = pageNumber;
        }
    }
}
