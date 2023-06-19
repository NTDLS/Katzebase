namespace Katzebase.Engine.Atomicity.Management
{
    /// <summary>
    /// Internal class methods for handling query requests related to transactions.
    /// </summary>
    internal class TransactionQueryHandlers
    {
        private readonly Core core;

        public TransactionQueryHandlers(Core core)
        {
            this.core = core;
        }

        public Transaction Begin(ulong processId)
        {
            return core.Transactions.Acquire(processId, true);
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
