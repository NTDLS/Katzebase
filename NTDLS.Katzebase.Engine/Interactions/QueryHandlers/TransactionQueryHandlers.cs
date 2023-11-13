
using NTDLS.Katzebase.Engine.Interactions.Management;

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
                _core.Log.Write($"Failed to instantiate transaction query handler.", ex);
                throw;
            }
        }

        public TransactionReference Begin(ulong processId) => _core.Transactions.Acquire(processId, true);
        public void Commit(ulong processId) => _core.Transactions.Commit(processId);
        public void Rollback(ulong processId) => _core.Transactions.Rollback(processId);
    }
}
