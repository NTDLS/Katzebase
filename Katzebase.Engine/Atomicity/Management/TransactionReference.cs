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

        internal TransactionReference(Transaction transaction)
        {
            this.Transaction = transaction;
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
