using Katzebase.PublicLibrary.Payloads;

namespace Katzebase.Engine.Atomicity.Management
{
    public class TransactionReference : IDisposable
    {
        internal Transaction Transaction { get; private set; }

        private bool isComittedOrRolledBack = false;

        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Rollback()
        {
            if (isComittedOrRolledBack == false)
            {
                isComittedOrRolledBack = true;
                Transaction.Rollback();
            }
        }

        public void Commit()
        {
            if (isComittedOrRolledBack == false)
            {
                isComittedOrRolledBack = true;
                if (Transaction.Commit())
                {
                    Transaction.Dispose();
                }
            }
        }

        public KbActionResponse CommitAndApplyMetricsThenReturnResults(int rowCount)
        {
            Commit();

            return new KbActionResponse
            {
                RowCount = rowCount,
                Metrics = Transaction.PT?.ToCollection(),
                Messages = Transaction.Messages,
                Warnings = Transaction.Warnings,
                Duration = (DateTime.UtcNow - Transaction.StartTime).TotalMilliseconds
            };
        }

        public KbActionResponse CommitAndApplyMetricsThenReturnResults()
        {
            return CommitAndApplyMetricsThenReturnResults(0);
        }

        public T CommitAndApplyMetricsThenReturnResults<T>(T result, int rowCount) where T : KbBaseActionResponse
        {
            Commit();

            result.RowCount = rowCount;
            result.Metrics = Transaction.PT?.ToCollection();
            result.Messages = Transaction.Messages;
            result.Warnings = Transaction.Warnings;
            result.Duration = (DateTime.UtcNow - Transaction.StartTime).TotalMilliseconds;

            return result;
        }

        internal TransactionReference(Transaction transaction)
        {
            Transaction = transaction;
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                //Rollback Transaction if its still open:
                if (isComittedOrRolledBack == false)
                {
                    isComittedOrRolledBack = true;
                    Transaction.Rollback();
                }
            }

            disposed = true;
        }
    }

}
