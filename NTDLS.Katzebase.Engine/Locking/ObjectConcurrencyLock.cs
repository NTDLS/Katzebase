namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectConcurrencyLock()
    {
        public int ReferenceCount { get; set; } = 1;
        public SemaphoreSlim Semaphore { get; set; } = new SemaphoreSlim(1, 1);
    }
}
