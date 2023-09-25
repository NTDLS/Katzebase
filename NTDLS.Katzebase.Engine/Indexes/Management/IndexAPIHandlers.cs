using NTDLS.Katzebase.Payloads;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Indexes.Management
{
    /// <summary>
    /// Public class methods for handling API requests related to indexes.
    /// </summary>
    public class IndexAPIHandlers
    {
        private readonly Core core;

        public IndexAPIHandlers(Core core)
        {
            this.core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate index API handlers.", ex);
                throw;
            }
        }

        public KbActionResponseIndex Get(ulong processId, string schemaName, string indexName)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                var indexCatalog = core.Indexes.AcquireIndexCatalog(transactionReference.Transaction, schemaName, LockOperation.Read);

                var physicalIndex = indexCatalog.GetByName(indexName);
                KbIndex? indexPayload = null;

                if (physicalIndex != null)
                {
                    indexPayload = PhysicalIndex.ToClientPayload(physicalIndex);
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbActionResponseIndex(indexPayload), 0);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        public KbActionResponseIndexes ListIndexes(ulong processId, string schemaName)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                var result = new KbActionResponseIndexes();

                var indexCatalog = core.Indexes.AcquireIndexCatalog(transactionReference.Transaction, schemaName, LockOperation.Read);
                if (indexCatalog != null)
                {
                    result.List.AddRange(indexCatalog.Collection.Select(o => PhysicalIndex.ToClientPayload(o)));
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result, 0);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to list indexes for process {processId}.", ex);
                throw;
            }
        }

        public KbActionResponseBoolean DoesIndexExist(ulong processId, string schemaName, string indexName)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                var indexCatalog = core.Indexes.AcquireIndexCatalog(transactionReference.Transaction, schemaName, LockOperation.Read);
                bool value = indexCatalog.GetByName(indexName) != null;
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbActionResponseBoolean(value), 0);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        public KbActionResponseGuid CreateIndex(ulong processId, string schemaName, KbIndex index)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                core.Indexes.CreateIndex(transactionReference.Transaction, schemaName, index, out Guid newId);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbActionResponseGuid(newId), 0);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        public KbActionResponse RebuildIndex(ulong processId, string schemaName, string indexName, uint newPartitionCount)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                core.Indexes.RebuildIndex(transactionReference.Transaction, schemaName, indexName, newPartitionCount);
                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        public KbActionResponse DropIndex(ulong processId, string schemaName, string indexName)
        {
            try
            {
                using var transactionReference = core.Transactions.Acquire(processId);
                core.Indexes.DropIndex(transactionReference.Transaction, schemaName, indexName);
                return transactionReference.CommitAndApplyMetricsThenReturnResults();
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }
    }
}
