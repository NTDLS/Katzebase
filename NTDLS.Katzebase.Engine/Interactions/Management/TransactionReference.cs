using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    public class TransactionReference : IDisposable
    {
        internal Transaction Transaction { get; private set; }

        private bool _isComittedOrRolledBack = false;
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Rollback()
        {
            if (_isComittedOrRolledBack == false)
            {
                _isComittedOrRolledBack = true;
                Transaction.Rollback();
            }
        }

        public void Commit()
        {
            if (_isComittedOrRolledBack == false)
            {
                _isComittedOrRolledBack = true;
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
                Warnings = Transaction.CloneWarnings(),
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
            result.Warnings = Transaction.CloneWarnings();
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
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                //Rollback Transaction if its still open:
                if (_isComittedOrRolledBack == false)
                {
                    _isComittedOrRolledBack = true;
                    Transaction.Rollback();
                }
            }

            _disposed = true;
        }
    }

}
