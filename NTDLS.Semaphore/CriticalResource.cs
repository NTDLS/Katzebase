namespace NTDLS.Semaphore
{
    /// <summary>
    /// Protects a variable from parallel / non-sequential thread access.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CriticalResource<T> : ICriticalResource where T : class, new()
    {
        private readonly T _value;

        public delegate void CriticalResourceDelegateWithVoidResult(T obj);
        public delegate R? CriticalResourceDelegateWithNullableResultT<R>(T obj);
        public delegate R CriticalResourceDelegateWithNotNullableResultT<R>(T obj);

        /// <summary>
        /// Identified the current thread that owns the lock.
        /// </summary>
        public Thread? Owner { get; private set; }

        public CriticalResource()
        {
            _value = new T();
        }

        public R Use<R>(CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            R result;

            try
            {
                Acquire();
                result = function(_value);
            }
            finally
            {
                Release();
            }

            return result;
        }

        public R? UseNullable<R>(CriticalResourceDelegateWithNullableResultT<R> function)
        {
            R? result;

            try
            {
                Acquire(); ;
                result = function(_value);
            }
            finally
            {
                Release();
            }

            return result;
        }

        private class LockObject
        {
            public ICriticalResource Resource { get; set; }
            public bool IsLockHeld { get; set; } = false;

            public LockObject(ICriticalResource resource)
            {
                Resource = resource;
            }
        }

        public void TryUseAll(ICriticalResource[] resources, out bool IsLockHeld, CriticalResourceDelegateWithVoidResult function)
        {
            var lockObjects = new LockObject[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < lockObjects.Length; i++)
                    {
                        lockObjects[i] = new(resources[i]);
                        lockObjects[i].IsLockHeld = lockObjects[i].Resource.TryAcquire();

                        if (lockObjects[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var _lockObject in lockObjects.Where(o => o.IsLockHeld))
                            {
                                lockObjects[i].Resource.Release();
                                _lockObject.IsLockHeld = false;
                            }

                            IsLockHeld = false;

                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in lockObjects.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    IsLockHeld = true;

                    return;
                }
                finally
                {
                    Release();
                }
            }

            IsLockHeld = false;
        }

        public void TryUseAll(ICriticalResource[] resources, int timeout, out bool IsLockHeld, CriticalResourceDelegateWithVoidResult function)
        {
            var lockObjects = new LockObject[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < lockObjects.Length; i++)
                    {
                        lockObjects[i] = new(resources[i]);
                        lockObjects[i].IsLockHeld = lockObjects[i].Resource.TryAcquire(timeout);

                        if (lockObjects[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var _lockObject in lockObjects.Where(o => o.IsLockHeld))
                            {
                                lockObjects[i].Resource.Release();
                                _lockObject.IsLockHeld = false;
                            }
                            IsLockHeld = false;

                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in lockObjects.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    IsLockHeld = true;

                    return;
                }
                finally
                {
                    Release();
                }
            }

            IsLockHeld = false;
        }

        public R? TryUseAll<R>(ICriticalResource[] resources, out bool IsLockHeld, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var lockObjects = new LockObject[resources.Length];

            R? result = default;

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < lockObjects.Length; i++)
                    {
                        lockObjects[i] = new(resources[i]);
                        lockObjects[i].IsLockHeld = lockObjects[i].Resource.TryAcquire();

                        if (lockObjects[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var _lockObject in lockObjects.Where(o => o.IsLockHeld))
                            {
                                lockObjects[i].Resource.Release();
                                _lockObject.IsLockHeld = false;
                            }

                            IsLockHeld = false;

                            return result;
                        }
                    }

                    result = function(_value);

                    foreach (var lockObject in lockObjects.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    IsLockHeld = true;

                    return result;
                }
                finally
                {
                    Release();
                }
            }

            IsLockHeld = false;

            return result;
        }

        public R? TryUseAll<R>(ICriticalResource[] resources, int timeout, out bool IsLockHeld, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var lockObjects = new LockObject[resources.Length];

            R? result = default;

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < lockObjects.Length; i++)
                    {
                        lockObjects[i] = new(resources[i]);
                        lockObjects[i].IsLockHeld = lockObjects[i].Resource.TryAcquire(timeout);

                        if (lockObjects[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var _lockObject in lockObjects.Where(o => o.IsLockHeld))
                            {
                                lockObjects[i].Resource.Release();
                                _lockObject.IsLockHeld = false;
                            }
                            IsLockHeld = false;

                            return default;
                        }
                    }

                    result = function(_value);

                    foreach (var lockObject in lockObjects.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    IsLockHeld = true;

                    return result;
                }
                finally
                {
                    Release();
                }
            }

            IsLockHeld = false;

            return result;
        }

        public R? UseAllNullable<R>(ICriticalResource[] resources, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            Acquire();

            R? result;

            try
            {
                foreach (var res in resources)
                {
                    res.Acquire();
                }

                result = function(_value);

                foreach (var res in resources)
                {
                    res.Release();
                }
            }
            finally
            {
                Release();
            }

            return result;
        }

        public R UseAll<R>(ICriticalResource[] resources, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            Acquire();

            R result;

            try
            {
                foreach (var res in resources)
                {
                    res.Acquire();
                }

                result = function(_value);

                foreach (var res in resources)
                {
                    res.Release();
                }
            }
            finally
            {
                Release();
            }

            return result;
        }

        public void UseAll(ICriticalResource[] resources, CriticalResourceDelegateWithVoidResult function)
        {
            Acquire();
            try
            {
                foreach (var res in resources)
                {
                    res.Acquire();
                }

                function(_value);

                foreach (var res in resources)
                {
                    res.Release();
                }
            }
            finally
            {
                Release();
            }
        }


        public void Use(CriticalResourceDelegateWithVoidResult function)
        {
            try
            {
                Acquire();
                function(_value);
            }
            finally
            {
                Release();
            }
        }

        public void TryUse(out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire();
                if (wasLockObtained)
                {
                    function(_value);
                    return;
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release();
                }
            }
        }

        public void TryUse(out bool wasLockObtained, int timeout, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(timeout);
                if (wasLockObtained)
                {
                    function(_value);
                    return;
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release();
                }
            }
        }

        public R? TryUseNullable<R>(out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire();
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release();
                }
            }
            return default;
        }

        public R? TryUseNullable<R>(out bool wasLockObtained, int timeout, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(timeout);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release();
                }
            }

            return default;
        }

        public R TryUse<R>(out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire();
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release();
                }
            }
            return defaultValue;
        }

        public R TryUse<R>(out bool wasLockObtained, R defaultValue, int timeout, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(timeout);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release();
                }
            }

            return defaultValue;
        }

        public bool TryAcquire(int timeout)
        {
            if (Monitor.TryEnter(this, timeout))
            {
                Owner = Thread.CurrentThread;
                return true;
            }
            return false;
        }

        public bool TryAcquire()
        {
            if (Monitor.TryEnter(this))
            {
                Owner = Thread.CurrentThread;
                return true;
            }
            return false;
        }

        public void Acquire()
        {
            Monitor.Enter(this);
            Owner = Thread.CurrentThread;
        }

        public void Release()
        {
            Owner = null;
            Monitor.Exit(this);
        }
    }
}
