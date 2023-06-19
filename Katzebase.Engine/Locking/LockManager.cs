using Katzebase.Engine.KbLib;

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
            Locks = new ObjectLocks(core);
        }

        internal void Remove(ObjectLock objectLock)
        {
            lock (CriticalSections.AcquireLock)
            {
                try
                {
                    Locks.Remove(objectLock);
                }
                catch (Exception ex)
                {
                    core.Log.Write("Failed to remove lock.", ex);
                    throw;
                }
            }
        }
    }
}
