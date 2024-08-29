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
                Management.LogManager.Error($"Failed to instantiate transaction API handlers.", ex);
                throw;
            }
        }

        public KbQueryTransactionBeginReply Begin(RmContext context, KbQueryTransactionBegin param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            Management.LogManager.Debug(Thread.CurrentThread.Name);
#endif
            _core.Transactions.Acquire(session, true);
            return new KbQueryTransactionBeginReply();
        }

        public KbQueryTransactionCommitReply Commit(RmContext context, KbQueryTransactionCommit param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            Management.LogManager.Debug(Thread.CurrentThread.Name);
#endif
            _core.Transactions.Commit(session.ProcessId);
            return new KbQueryTransactionCommitReply();
        }

        public KbQueryTransactionRollbackReply Rollback(RmContext context, KbQueryTransactionRollback param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            Management.LogManager.Debug(Thread.CurrentThread.Name);
#endif
            _core.Transactions.Rollback(session.ProcessId);
            return new KbQueryTransactionRollbackReply();
        }
    }
}
