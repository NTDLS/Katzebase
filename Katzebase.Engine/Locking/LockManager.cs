using Katzebase.Engine.Library;

namespace Katzebase.Engine.Locking
{
    /// <summary>
    /// Internal core class methods for locking, reading, writing and managing tasks related to locking.
    /// </summary>
    internal class LockManager
    {
        internal ObjectLocks Locks { get; set; }
        private readonly Core core;

        internal LockManager(Core core)
        {
            this.core = core;
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
                core.Log.Write($"Failed to remove lock.", ex);
                throw;
            }
        }
    }
}
