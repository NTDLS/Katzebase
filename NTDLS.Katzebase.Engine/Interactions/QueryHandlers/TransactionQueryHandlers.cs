
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Sessions;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to transactions.
    /// </summary>
    internal class TransactionQueryHandlers<TData> where TData : IStringable
    {
        private readonly EngineCore<TData> _core;

        public TransactionQueryHandlers(EngineCore<TData> core)
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

        public TransactionReference<TData> Begin(SessionState session) => _core.Transactions.Acquire(session, true);
        public void Commit(SessionState session) => _core.Transactions.Commit(session);
        public void Rollback(SessionState session) => _core.Transactions.Rollback(session);
    }
}
