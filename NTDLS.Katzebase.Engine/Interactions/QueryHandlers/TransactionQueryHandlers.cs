using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Sessions;

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

        public TransactionReference Begin(SessionState session) => _core.Transactions.Acquire(session, true);
        public void Commit(SessionState session) => _core.Transactions.Commit(session);
        public void Rollback(SessionState session) => _core.Transactions.Rollback(session);
    }
}
