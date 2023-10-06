namespace NTDLS.Katzebase.Engine.Library
{
    internal static class StaticCriticalSections
    {
        //TODO evaluate the useage of this lock. I think its over used.
        internal static CriticalSectionManager AcquireLock { get; } = new();
    }
}
