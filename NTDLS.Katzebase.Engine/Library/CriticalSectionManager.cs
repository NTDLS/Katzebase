namespace NTDLS.Katzebase.Engine.Library
{
    internal class CriticalSectionManager
    {
        /// <summary>
        /// Enters a critical section and returns a IDisposable object for which its disposal will release the lock.
        /// </summary>
        /// <returns></returns>
        public CriticalSection Enter()
        {
            return new CriticalSection(this);
        }

        /// <summary>
        /// Attempts to enter a critical section for a given amount of time and returns a IDisposable object for which its disposal will release the lock.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="wasLockAcquired"></param>
        /// <returns></returns>
        public CriticalSection TryEnter(int timeout, out bool wasLockAcquired)
        {
            var result = new CriticalSection(this, timeout);
            wasLockAcquired = result.IsLockHeld;
            return result;
        }

        /// <summary>
        /// Attempts to enter a critical section and returns a IDisposable object for which its disposal will release the lock.
        /// </summary>
        /// <param name="wasLockAcquired"></param>
        /// <returns></returns>
        public CriticalSection TryEnter(out bool wasLockAcquired)
        {
            var result = new CriticalSection(this, 0);
            wasLockAcquired = result.IsLockHeld;
            return result;
        }
    }
}
