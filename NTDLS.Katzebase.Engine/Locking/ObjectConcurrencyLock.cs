namespace NTDLS.Katzebase.Engine.Locking
{
    class ObjectConcurrencyLock()
    {
        public int ReferenceCount { get; set; } = 1;
    }
}
