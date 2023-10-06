using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Engine.Locking;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Internal core class methods for locking, reading, writing and managing tasks related to locking.
    /// </summary>
    internal class LockManager
    {
        internal ObjectLocks Locks { get; private set; }
        private readonly EngineCore _core;

        internal LockManager(EngineCore core)
        {
            _core = core;
            try
            {
                Locks = new ObjectLocks(core);
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instantiate lock manager.", ex);
                throw;
            }
        }

        internal void Remove(ObjectLock objectLock)
        {
            using (StaticCriticalSections.AcquireLock.Enter())
            {
                try
                {
                    Locks.Remove(objectLock);
                }
                catch (Exception ex)
                {
                    _core.Log.Write($"Failed to remove lock.", ex);
                    throw;
                }
            }
        }
    }
}
