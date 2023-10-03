namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to transactions.
    /// </summary>
    public class TransactiontAPIHandlers
    {
        private readonly Core _core;

        public TransactiontAPIHandlers(Core core)
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

        public void Begin(ulong processId) => _core.Transactions.Acquire(processId, true);
        public void Commit(ulong processId) => _core.Transactions.Commit(processId);
        public void Rollback(ulong processId) => _core.Transactions.Rollback(processId);
    }
}
