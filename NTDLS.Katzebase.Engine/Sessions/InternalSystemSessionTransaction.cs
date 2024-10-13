using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Interactions.Management;

namespace NTDLS.Katzebase.Engine.Sessions
{
    /// <summary>
    /// Provides an easy way to get a system process and transaction that cleans itself up when you are done with it.
    /// </summary>
    internal class InternalSystemSessionTransaction(EngineCore core, SessionState session, TransactionReference transactionReference) : IDisposable
    {
        public EngineCore Core { get; private set; } = core;
        public SessionState Session { get; private set; } = session;
        public TransactionReference TransactionReference { get; private set; } = transactionReference;
        public Transaction Transaction => TransactionReference.Transaction;

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
