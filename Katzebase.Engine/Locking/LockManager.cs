using Katzebase.Engine.KbLib;

namespace Katzebase.Engine.Locking
{
    public class LockManager
    {
        public ObjectLocks Locks { get; set; }
        private Core core;

        public LockManager(Core core)
        {
            this.core = core;
            Locks = new ObjectLocks(core);
        }

        public void Remove(ObjectLock objectLock)
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
