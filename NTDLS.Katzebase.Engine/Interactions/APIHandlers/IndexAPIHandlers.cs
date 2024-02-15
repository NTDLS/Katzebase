using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Client.Payloads.RoundTrip;
using NTDLS.Katzebase.Engine.Indexes;
using NTDLS.ReliableMessaging;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to indexes.
    /// </summary>
    public class IndexAPIHandlers : IRmMessageHandler
    {
        private readonly EngineCore _core;

        public IndexAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instantiate index API handlers.", ex);
                throw;
            }
        }

        public KbQueryIndexGetReply Get(RmContext context, KbQueryIndexGet param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var indexCatalog = _core.Indexes.AcquireIndexCatalog(transactionReference.Transaction, param.Schema, LockOperation.Read);

                var physicalIndex = indexCatalog.GetByName(param.IndexName);
                KbIndex? indexPayload = null;

                if (physicalIndex != null)
                {
                    indexPayload = PhysicalIndex.ToClientPayload(physicalIndex);
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryIndexGetReply(indexPayload), 0);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        public KbQueryIndexListReply ListIndexes(RmContext context, KbQueryIndexList param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var result = new KbQueryIndexListReply();

                var indexCatalog = _core.Indexes.AcquireIndexCatalog(transactionReference.Transaction, param.Schema, LockOperation.Read);
                if (indexCatalog != null)
                {
                    result.List.AddRange(indexCatalog.Collection.Select(o => PhysicalIndex.ToClientPayload(o)));
                }

                return transactionReference.CommitAndApplyMetricsThenReturnResults(result);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to list indexes for process {processId}.", ex);
                throw;
            }
        }

        public KbQueryIndexExistsReply DoesIndexExist(RmContext context, KbQueryIndexExists param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                var indexCatalog = _core.Indexes.AcquireIndexCatalog(transactionReference.Transaction, param.Schema, LockOperation.Read);
                bool value = indexCatalog.GetByName(param.IndexName) != null;
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryIndexExistsReply(value));
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        public KbQueryIndexCreateReply CreateIndex(RmContext context, KbQueryIndexCreate param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                _core.Indexes.CreateIndex(transactionReference.Transaction, param.Schema, param.Index, out Guid newId);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryIndexCreateReply(newId), 0);
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        public KbQueryIndexRebuildReply RebuildIndex(RmContext context, KbQueryIndexRebuild param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                _core.Indexes.RebuildIndex(transactionReference.Transaction, param.Schema, param.IndexName, param.NewPartitionCount);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryIndexRebuildReply());
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }

        public KbQueryIndexDropReply DropIndex(RmContext context, KbQueryIndexDrop param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.Acquire(processId);
                _core.Indexes.DropIndex(transactionReference.Transaction, param.Schema, param.IndexName);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryIndexDropReply());
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to create index for process {processId}.", ex);
                throw;
            }
        }
    }
}
