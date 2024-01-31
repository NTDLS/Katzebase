using NTDLS.Katzebase.Client.Payloads.Queries;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to transactions.
    /// </summary>
    public class TransactiontAPIHandlers
    {
        private readonly EngineCore _core;

        public TransactiontAPIHandlers(EngineCore core)
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

        public KbQueryTransactionCommitReply Begin(ulong processId)
        {
            _core.Transactions.Acquire(processId, true);
            return new KbQueryTransactionCommitReply()
            {
                Success = true,
            };
        }

        public KbQueryTransactionCommitReply Commit(ulong processId)
        {
            _core.Transactions.Commit(processId);
            return new KbQueryTransactionCommitReply()
            {
                Success = true,
            };
        }

        public KbQueryTransactionRollbackReply Rollback(ulong processId)
        {
            _core.Transactions.Rollback(processId);
            return new KbQueryTransactionRollbackReply()
            {
                Success = true,
            };
        }
    }
}
