namespace NTDLS.Katzebase.Engine.Library
{
    internal class CriticalSection
    {
        /// <summary>
        /// Enters a critical section and returns a IDisposable object for which its disposal will release the lock.
        /// </summary>
        /// <returns></returns>
        public CriticalSectionReference Lock()
        {
            return new CriticalSectionReference(this);
        }

        public static CriticalSectionReference Lock(object obj) => new CriticalSectionReference(obj);

        public static CriticalSectionReference TryLock(object obj, int timeout, out bool wasLockAcquired)
        {
            var result = new CriticalSectionReference(obj, timeout);
            wasLockAcquired = result.IsLockHeld;
            return result;
        }

        public static CriticalSectionReference TryLock(object obj, out bool wasLockAcquired)
        {
            var result = new CriticalSectionReference(obj, 0);
            wasLockAcquired = result.IsLockHeld;
            return result;
        }

        public delegate void ManagedCriticalSectionDelegateT<T>(T obj);

        public static void Execute<T>(T obj, ManagedCriticalSectionDelegateT<T> function) where T : class
        {
            using (new CriticalSectionReference(obj))
            {
                function(obj);
            }
        }

        public static bool TryExecute<T>(T obj, ManagedCriticalSectionDelegateT<T> function) where T : class
        {
            using (var crit = new CriticalSectionReference(obj, 0))
            {
                if (crit.IsLockHeld)
                {
                    function(obj);
                }
                return crit.IsLockHeld;
            }
        }

        public static bool TryExecute<T>(T obj, int timeout, ManagedCriticalSectionDelegateT<T> function) where T : class
        {
            using (var crit = new CriticalSectionReference(obj, timeout))
            {
                if (crit.IsLockHeld)
                {
                    function(obj);
                }
                return crit.IsLockHeld;
            }
        }

        public delegate void ManagedCriticalSectionDelegate();

        public static void Execute(object obj, ManagedCriticalSectionDelegate function)
        {
            using (new CriticalSectionReference(obj))
            {
                function();
            }
        }

        public static bool TryExecute(object obj, ManagedCriticalSectionDelegate function)
        {
            using (var crit = new CriticalSectionReference(obj, 0))
            {
                if (crit.IsLockHeld)
                {
                    function();
                }
                return crit.IsLockHeld;
            }
        }

        public static bool TryExecute(object obj, int timeout, ManagedCriticalSectionDelegate function)
        {
            using (var crit = new CriticalSectionReference(obj, timeout))
            {
                if (crit.IsLockHeld)
                {
                    function();
                }
                return crit.IsLockHeld;
            }
        }

        public static void Execute(object[] objs, ManagedCriticalSectionDelegate function)
        {
            using (new CriticalSectionReferenceArray(objs))
            {
                function();
            }
        }

        public static bool TryExecute(object[] obj, ManagedCriticalSectionDelegate function)
        {
            using (var crit = new CriticalSectionReferenceArray(obj, 0))
            {
                if (crit.IsLockHeld)
                {
                    function();
                }
                return crit.IsLockHeld;
            }
        }

        public static bool TryExecute(object[] obj, int timeout, ManagedCriticalSectionDelegate function)
        {
            using (var crit = new CriticalSectionReferenceArray(obj, timeout))
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
        public CriticalSectionReference TryLock(int timeout, out bool wasLockAcquired)
        {
            var result = new CriticalSectionReference(this, timeout);
            wasLockAcquired = result.IsLockHeld;
            return result;
        }

        /// <summary>
        /// Attempts to enter a critical section and returns a IDisposable object for which its disposal will release the lock.
        /// </summary>
        /// <param name="wasLockAcquired"></param>
        /// <returns></returns>
        public CriticalSectionReference TryLock(out bool wasLockAcquired)
        {
            var result = new CriticalSectionReference(this, 0);
            wasLockAcquired = result.IsLockHeld;
            return result;
        }
    }
}
