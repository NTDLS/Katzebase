using Katzebase.Engine.KbLib;

namespace Katzebase.Engine.Locking
{
    internal class LockManager
    {
        internal ObjectLocks Locks { get; set; }
        private Core core;

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
