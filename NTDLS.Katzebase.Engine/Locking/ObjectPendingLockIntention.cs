using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectPendingLockIntention(Transaction<TData> transaction, ObjectLockIntention intention)
    {
        public Transaction<TData> transaction { get; set; } = transaction;
        public ObjectLockIntention Intention { get; set; } = intention;
        public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;
    }
}
