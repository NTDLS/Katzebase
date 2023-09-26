using NTDLS.Katzebase.Engine.Library;

namespace NTDLS.Katzebase.Engine.Locking
{
    /// <summary>
    /// Internal core class methods for locking, reading, writing and managing tasks related to locking.
    /// </summary>
    internal class LockManager
    {
        internal ObjectLocks Locks { get; private set; }
        private readonly Core _core;

        internal LockManager(Core core)
        {
            _core = core;
            try
            {
                Locks = new ObjectLocks(core);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate lock manager.", ex);
                throw;
            }
        }

        internal void Remove(ObjectLock objectLock)
        {
            try
            {
                lock (CentralCriticalSections.AcquireLock)
                {
                    Locks.Remove(objectLock);
                }
            }
            catch (Exception ex)
            {
                _core.Log.Write($"Failed to remove lock.", ex);
                throw;
            }
        }
    }
}
