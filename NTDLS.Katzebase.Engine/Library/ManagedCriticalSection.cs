namespace NTDLS.Katzebase.Engine.Library
{
    internal class ManagedCriticalSection
    {
        /// <summary>
        /// Enters a critical section and returns a IDisposable object for which its disposal will release the lock.
        /// </summary>
        /// <returns></returns>
        public CriticalSection Lock()
        {
            return new CriticalSection(this);
        }

        public static CriticalSection Lock(object obj) => new CriticalSection(obj);

        public static CriticalSection TryLock(object obj, int timeout, out bool wasLockAcquired)
        {
            var result = new CriticalSection(obj, timeout);
            wasLockAcquired = result.IsLockHeld;
            return result;
        }

        public static CriticalSection TryLock(object obj, out bool wasLockAcquired)
        {
            var result = new CriticalSection(obj, 0);
            wasLockAcquired = result.IsLockHeld;
            return result;
        }

        public delegate void ManagedCriticalSectionDelegateT<T>(T obj);

        public static void LockAndExecute<T>(T obj, ManagedCriticalSectionDelegateT<T> function) where T : class
        {
            using (new CriticalSection(obj))
            {
                function(obj);
            }
        }

        public static bool TryLockAndExecute<T>(T obj, ManagedCriticalSectionDelegateT<T> function) where T : class
        {
            using (var crit = new CriticalSection(obj, 0))
            {
                if (crit.IsLockHeld)
                {
                    function(obj);
                }
                return crit.IsLockHeld;
            }
        }

        public static bool TryLockAndExecute<T>(T obj, int timeout, ManagedCriticalSectionDelegateT<T> function) where T : class
        {
            using (var crit = new CriticalSection(obj, timeout))
            {
                if (crit.IsLockHeld)
                {
                    function(obj);
                }
                return crit.IsLockHeld;
            }
        }

        public delegate void ManagedCriticalSectionDelegate();

        public static void LockAndExecute(object obj, ManagedCriticalSectionDelegate function)
        {
            using (new CriticalSection(obj))
            {
                function();
            }
        }

        public static bool TryLockAndExecute(object obj, ManagedCriticalSectionDelegate function)
        {
            using (var crit = new CriticalSection(obj, 0))
            {
                if (crit.IsLockHeld)
                {
                    function();
                }
                return crit.IsLockHeld;
            }
        }

        public static bool TryLockAndExecute(object obj, int timeout, ManagedCriticalSectionDelegate function)
        {
            using (var crit = new CriticalSection(obj, timeout))
            {
                if (crit.IsLockHeld)
                {
                    function();
                }
                return crit.IsLockHeld;
            }
        }

        /// <summary>
        /// Attempts to enter a critical section for a given amount of time and returns a IDisposable object for which its disposal will release the lock.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="wasLockAcquired"></param>
        /// <returns></returns>
        public CriticalSection TryLock(int timeout, out bool wasLockAcquired)
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
        public CriticalSection TryLock(out bool wasLockAcquired)
        {
            var result = new CriticalSection(this, 0);
            wasLockAcquired = result.IsLockHeld;
            return result;
        }
    }
}
