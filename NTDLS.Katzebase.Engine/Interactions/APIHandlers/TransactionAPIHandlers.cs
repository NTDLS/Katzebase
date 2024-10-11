using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.ReliableMessaging;
using System.Diagnostics;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to transactions.
    /// </summary>
    public class TransactionAPIHandlers : IRmMessageHandler
    {
        private readonly EngineCore _core;

        public TransactionAPIHandlers(EngineCore core)
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
            try
            {
                _core.Transactions.Acquire(session, true);
                var apiResult = new KbQueryTransactionBeginReply();
                return apiResult;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public KbQueryTransactionCommitReply Commit(RmContext context, KbQueryTransactionCommit param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                _core.Transactions.Commit(session.ProcessId);

                var apiResult = new KbQueryTransactionCommitReply();
                return apiResult;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public KbQueryTransactionRollbackReply Rollback(RmContext context, KbQueryTransactionRollback param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                _core.Transactions.Rollback(session.ProcessId);

                var apiResult = new KbQueryTransactionRollbackReply();
                return apiResult;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
