using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectPendingLockIntention(Transaction transaction, ObjectLockIntention intention)
    {
        public Transaction Transaction { get; set; } = transaction;
        public ObjectLockIntention Intention { get; set; } = intention;
        public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;
    }
}
