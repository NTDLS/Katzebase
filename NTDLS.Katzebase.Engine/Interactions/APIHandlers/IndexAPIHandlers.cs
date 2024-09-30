using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Client.Payloads.RoundTrip;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.PersistentTypes.Index;
using NTDLS.ReliableMessaging;
using static NTDLS.Katzebase.Shared.EngineConstants;

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
                LogManager.Error($"Failed to instantiate index API handlers.", ex);
                throw;
            }
        }

        public KbQueryIndexGetReply Get(RmContext context, KbQueryIndexGet param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
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
                LogManager.Error($"Failed to create index for process {session.ProcessId}.", ex);
                throw;
            }
        }

        public KbQueryIndexListReply ListIndexes(RmContext context, KbQueryIndexList param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
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
                LogManager.Error($"Failed to list indexes for process {session.ProcessId}.", ex);
                throw;
            }
        }

        public KbQueryIndexExistsReply DoesIndexExist(RmContext context, KbQueryIndexExists param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                var indexCatalog = _core.Indexes.AcquireIndexCatalog(transactionReference.Transaction, param.Schema, LockOperation.Read);
                bool value = indexCatalog.GetByName(param.IndexName) != null;
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryIndexExistsReply(value));
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to create index for process {session.ProcessId}.", ex);
                throw;
            }
        }

        public KbQueryIndexCreateReply CreateIndex(RmContext context, KbQueryIndexCreate param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                _core.Indexes.CreateIndex(transactionReference.Transaction, param.Schema, param.Index, out Guid newId);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryIndexCreateReply(newId), 0);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to create index for process {session.ProcessId}.", ex);
                throw;
            }
        }

        public KbQueryIndexRebuildReply RebuildIndex(RmContext context, KbQueryIndexRebuild param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                _core.Indexes.RebuildIndex(transactionReference.Transaction, param.Schema, param.IndexName, param.NewPartitionCount);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryIndexRebuildReply());
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to create index for process {session.ProcessId}.", ex);
                throw;
            }
        }

        public KbQueryIndexDropReply DropIndex(RmContext context, KbQueryIndexDrop param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                using var transactionReference = _core.Transactions.Acquire(session);
                _core.Indexes.DropIndex(transactionReference.Transaction, param.Schema, param.IndexName);
                return transactionReference.CommitAndApplyMetricsThenReturnResults(new KbQueryIndexDropReply());
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to create index for process {session.ProcessId}.", ex);
                throw;
            }
        }
    }
}
