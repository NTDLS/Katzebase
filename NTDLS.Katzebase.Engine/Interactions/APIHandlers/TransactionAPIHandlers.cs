using NTDLS.Katzebase.Client.Payloads.RoundTrip;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.ReliableMessaging;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to transactions.
    /// </summary>
    public class TransactionAPIHandlers<TData> : IRmMessageHandler where TData : IStringable
    {
        private readonly EngineCore<TData> _core;

        public TransactionAPIHandlers(EngineCore<TData> core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate transaction API handlers.", ex);
                throw;
            }
        }

        public KbQueryTransactionBeginReply Begin(RmContext context, KbQueryTransactionBegin param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            _core.Transactions.Acquire(session, true);
            return new KbQueryTransactionBeginReply();
        }

        public KbQueryTransactionCommitReply Commit(RmContext context, KbQueryTransactionCommit param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            _core.Transactions.Commit(session.ProcessId);
            return new KbQueryTransactionCommitReply();
        }

        public KbQueryTransactionRollbackReply Rollback(RmContext context, KbQueryTransactionRollback param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            _core.Transactions.Rollback(session.ProcessId);
            return new KbQueryTransactionRollbackReply();
        }
    }
}
