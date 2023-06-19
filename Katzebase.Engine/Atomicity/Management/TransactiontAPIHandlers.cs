using Katzebase.Engine.Atomicity;

namespace Katzebase.Engine.Atomicity.Management
{
    public class TransactiontAPIHandlers
    {
        private readonly Core core;

        public TransactiontAPIHandlers(Core core)
        {
            this.core = core;
        }

        public void Acquire(ulong processId)
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
