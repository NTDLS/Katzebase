using NTDLS.Katzebase.Client.Payloads.RoundTrip;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to transactions.
    /// </summary>
    public class TransactiontAPIHandlers : IRmMessageHandler
    {
        private readonly EngineCore _core;

        public TransactiontAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instantiate transaction API handlers.", ex);
                throw;
            }
        }

        public KbQueryTransactionBeginReply Begin(RmContext context, KbQueryTransactionBegin param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            _core.Transactions.Acquire(processId, true);
            return new KbQueryTransactionBeginReply()
            {
                Success = true,
            };
        }

        public KbQueryTransactionCommitReply Commit(RmContext context, KbQueryTransactionCommit param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            _core.Transactions.Commit(processId);
            return new KbQueryTransactionCommitReply()
            {
                Success = true,
            };
        }

        public KbQueryTransactionRollbackReply Rollback(RmContext context, KbQueryTransactionRollback param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            _core.Transactions.Rollback(processId);
            return new KbQueryTransactionRollbackReply()
            {
                Success = true,
            };
        }
    }
}
