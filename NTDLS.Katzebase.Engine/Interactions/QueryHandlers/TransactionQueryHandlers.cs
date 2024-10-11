using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Sessions;
using System.Diagnostics;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to transactions.
    /// </summary>
    internal class TransactionQueryHandlers
    {
        private readonly EngineCore _core;

        public TransactionQueryHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate transaction query handler.", ex);
                throw;
            }
        }

        public TransactionReference Begin(SessionState session)
        {
            try
            {
                return _core.Transactions.Acquire(session, true);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public void Commit(SessionState session)
        {
            try
            {
                _core.Transactions.Commit(session);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public void Rollback(SessionState session)
        {
            try
            {
                _core.Transactions.Rollback(session);
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
