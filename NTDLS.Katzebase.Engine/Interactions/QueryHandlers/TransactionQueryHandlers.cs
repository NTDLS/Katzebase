using NTDLS.Katzebase.Engine.Atomicity.Management;

namespace NTDLS.Katzebase.Engine.Interactions.QueryHandlers
{
    /// <summary>
    /// Internal class methods for handling query requests related to transactions.
    /// </summary>
    internal class TransactionQueryHandlers
    {
        private readonly Core _core;

        public TransactionQueryHandlers(Core core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate transaction query handler.", ex);
                throw;
            }
        }

        public TransactionReference Begin(ulong processId) => _core.Transactions.Acquire(processId, true);
        public void Commit(ulong processId) => _core.Transactions.Commit(processId);
        public void Rollback(ulong processId) => _core.Transactions.Rollback(processId);
    }
}
