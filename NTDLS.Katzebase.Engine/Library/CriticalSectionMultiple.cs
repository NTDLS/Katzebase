namespace NTDLS.Katzebase.Engine.Library
{
    internal class CriticalSectionMultiple : IDisposable
    {
        private class LockObject
        {
            public object Obj { get; set; }
            public bool IsLockHeld { get; set; } = false;

            public LockObject(object obj)
            {
                Obj = obj;
            }
        }

        private readonly LockObject[] _lockObjects;

        public bool IsLockHeld => _lockObjects.All(x => x.IsLockHeld);

        /// <summary>
        /// Enters a critical section.
        /// </summary>
        /// <param name="lockObject"></param>
        public CriticalSectionMultiple(object[] lockObjects)
        {
            _lockObjects = new LockObject[lockObjects.Length];

            for (int i = 0; i < _lockObjects.Length; i++)
            {
                _lockObjects[i] = new(lockObjects[i]);
                Monitor.Enter(_lockObjects[i].Obj);
                _lockObjects[i].IsLockHeld = true;
            }
        }

        /// <summary>
        /// Tries to enter a critical section.
        /// </summary>
        /// <param name="lockObject"></param>
        public CriticalSectionMultiple(object[] lockObjects, int timeout)
        {
            _lockObjects = new LockObject[lockObjects.Length];

            for (int i = 0; i < _lockObjects.Length; i++)
            {
                _lockObjects[i] = new(lockObjects[i]);
                _lockObjects[i].IsLockHeld = Monitor.TryEnter(_lockObjects[i].Obj, timeout);

                if (_lockObjects[i].IsLockHeld == false)
                {
                    //We didnt get one of the locks, free the ones we did get and bailout.
                    foreach (var _lockObject in _lockObjects.Where(o => o.IsLockHeld))
                    {
                        Monitor.Exit(_lockObjects[i].Obj);
                        _lockObject.IsLockHeld = false;
                    }

                    break;
                }
            }
        }

        public void Dispose()
        {
            foreach (var _lockObject in _lockObjects.Where(o => o.IsLockHeld))
            {
                Monitor.Exit(_lockObject.Obj);
                _lockObject.IsLockHeld = false;
            }
        }
    }
}
