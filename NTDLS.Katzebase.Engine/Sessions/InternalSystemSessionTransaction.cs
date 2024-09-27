using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.Management;

namespace NTDLS.Katzebase.Engine.Sessions
{
    /// <summary>
    /// Provides an easy way to get a system process and transaction that cleans itself up when you are done with it.
    /// </summary>
    internal class InternalSystemSessionTransaction<TData> : IDisposable where TData: IStringable
    {
        public EngineCore<TData> Core { get; set; }
        public SessionState Session { get; set; }
        public TransactionReference<TData> TransactionReference { get; set; }
        public Transaction<TData> Transaction => TransactionReference.Transaction;

        public InternalSystemSessionTransaction(EngineCore<TData> core, SessionState session, TransactionReference<TData> transactionReference)
        {
            Core = core;
            Session = session;
            TransactionReference = transactionReference;
        }

        public void Commit()
        {
            TransactionReference.Commit();
        }

        public void Rollback()
        {
            TransactionReference.Rollback();
        }

        public void Dispose()
        {
            //Rollback the transaction if it is still open.
            TransactionReference.Transaction.Rollback();
            Core.Sessions.CloseByProcessId(Session.ProcessId);
        }
    }
}
