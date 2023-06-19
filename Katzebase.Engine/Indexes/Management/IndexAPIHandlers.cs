using Katzebase.PublicLibrary.Payloads;
using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Indexes.Management
{
    public class IndexAPIHandlers
    {
        private readonly Core core;

        public IndexAPIHandlers(Core core)
        {
            this.core = core;
        }

        public KbActionResponseIndexes ListIndexes(ulong processId, string schemaName)
        {
            var result = new KbActionResponseIndexes();
            try
            {
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var indexCatalog = core.Indexes.AcquireIndexCatalog(transaction, schemaName, LockOperation.Read);
                    if (indexCatalog != null)
                    {
                        foreach (var index in indexCatalog.Collection)
                        {
                            result.Add(PhysicalIndex.ToClientPayload(index));
                        }
                    }


                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to list indexes for process {processId}.", ex);
                throw;
            }

            return result;
        }

        public bool DoesIndexExist(ulong processId, string schemaName, string indexName)
        {
            bool result = false;
            try
            {
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    var indexCatalog = core.Indexes.AcquireIndexCatalog(transaction, schemaName, LockOperation.Read);

                    result = indexCatalog.GetByName(indexName) != null;

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }

            return result;
        }

        public void CreateIndex(ulong processId, string schemaName, KbIndex index, out Guid newId)
        {
            try
            {
                var physicalIndex = PhysicalIndex.FromClientPayload(index);

                using (var transaction = core.Transactions.Acquire(processId))
                {
                    core.Indexes.CreateIndex(transaction, schemaName, index, out newId);
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        public void RebuildIndex(ulong processId, string schemaName, string indexName)
        {
            try
            {
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    core.Indexes.RebuildIndex(transaction, schemaName, indexName);
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        public void DropIndex(ulong processId, string schemaName, string indexName)
        {
            try
            {
                using (var transaction = core.Transactions.Acquire(processId))
                {
                    core.Indexes.DropIndex(transaction, schemaName, indexName);
                    transaction.Commit();
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
