namespace Katzebase.Engine.Library
{
    public static class CentralCriticalSections
    {
        public static object AcquireLock = new();
    }
}
