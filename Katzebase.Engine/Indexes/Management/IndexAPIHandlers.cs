using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.Library.EngineConstants;

namespace Katzebase.Engine.Indexes.Management
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

        public KbActionResponseIndexes ListIndexes(ulong processId, string schemaName)
        {
            try
            {
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponseIndexes();

                    var indexCatalog = core.Indexes.AcquireIndexCatalog(transaction, schemaName, LockOperation.Read);
                    if (indexCatalog != null)
                    {
                        result.List.AddRange(indexCatalog.Collection.Select(o => PhysicalIndex.ToClientPayload(o)));
                    }

                    transaction.Commit();
                    result.RowCount = 0;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
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
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponseBoolean();
                    var indexCatalog = core.Indexes.AcquireIndexCatalog(transaction, schemaName, LockOperation.Read);
                    result.Value = indexCatalog.GetByName(indexName) != null;

                    transaction.Commit();
                    result.RowCount = 0;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
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
                var physicalIndex = PhysicalIndex.FromClientPayload(index);

                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponseGuid();
                    core.Indexes.CreateIndex(transaction, schemaName, index, out Guid newId);
                    result.Id = newId;

                    transaction.Commit();
                    result.RowCount = 0;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        public KbActionResponse RebuildIndex(ulong processId, string schemaName, string indexName)
        {
            try
            {
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();

                    core.Indexes.RebuildIndex(transaction, schemaName, indexName);

                    transaction.Commit();
                    result.RowCount = 0;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
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
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var result = new KbActionResponse();

                    core.Indexes.DropIndex(transaction, schemaName, indexName);

                    transaction.Commit();
                    result.RowCount = 0;
                    result.Metrics = transaction.PT?.ToCollection();
                    result.Success = true;
                    return result;
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }
    }
}
