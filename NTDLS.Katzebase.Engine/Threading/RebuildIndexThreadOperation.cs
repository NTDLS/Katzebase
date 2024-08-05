using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.Katzebase.Engine.Schemas;

namespace NTDLS.Katzebase.Engine.Threading
{
    /// <summary>
    /// Thread parameters for a lookup operations. Shared across all threads in a single lookup operation.
    /// </summary>
    internal class RebuildIndexThreadOperation
    {
        public Transaction Transaction { get; set; }
        public PhysicalSchema PhysicalSchema { get; set; }
        public PhysicalIndex PhysicalIndex { get; set; }
        public Dictionary<uint, PhysicalIndexPages> PhysicalIndexPageMap { get; set; }
        public object[] SyncObjects { get; private set; }

        public RebuildIndexThreadOperation(Transaction transaction, PhysicalSchema physicalSchema,
            Dictionary<uint, PhysicalIndexPages> physicalIndexPageMap, PhysicalIndex physicalIndex, uint indexPartitions)
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
    }
}
