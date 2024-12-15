using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.ReliableMessaging;
using System.Diagnostics;
using static NTDLS.Katzebase.Shared.EngineConstants;

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
            using var trace = _core.Trace.CreateTracker(TraceType.TransactionBegin, context.ConnectionId);
            var session = _core.Sessions.GetSession(context.ConnectionId);
            trace.SetSession(session);

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
            using var trace = _core.Trace.CreateTracker(TraceType.TransactionCommit, context.ConnectionId);
            var session = _core.Sessions.GetSession(context.ConnectionId);
            trace.SetSession(session);

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
            using var trace = _core.Trace.CreateTracker(TraceType.TransactionRollback, context.ConnectionId);
            var session = _core.Sessions.GetSession(context.ConnectionId);
            trace.SetSession(session);

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
