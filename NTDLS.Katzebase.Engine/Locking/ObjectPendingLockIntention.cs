using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectPendingLockIntention<TData>(Transaction<TData> transaction, ObjectLockIntention intention)
        where TData : IStringable
    {
        public Transaction<TData> Transaction { get; set; } = transaction;
        public ObjectLockIntention Intention { get; set; } = intention;
        public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;
    }
}
