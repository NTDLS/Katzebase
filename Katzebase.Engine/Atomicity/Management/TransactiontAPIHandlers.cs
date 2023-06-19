using Katzebase.Engine.Atomicity;

namespace Katzebase.Engine.Atomicity.Management
{
    /// <summary>
    /// Public class methods for handling API requests related to transactions.
    /// </summary>
    public class TransactiontAPIHandlers
    {
        private readonly Core core;

        public TransactiontAPIHandlers(Core core)
        {
            this.core = core;
        }

        public void Begin(ulong processId)
        {
            core.Transactions.Acquire(processId, true);
        }

        public void Commit(ulong processId)
        {
            core.Transactions.Commit(processId);
        }

        public void Rollback(ulong processId)
        {
            core.Transactions.Rollback(processId);
        }
    }
}
