using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectLockKey
    {
        private readonly Core _core;
        public ObjectLock ObjectLock { get; private set; }
        public LockOperation LockOperation { get; private set; }
        public ulong ProcessId { get; private set; }

        public void TurnInKey()
        {
            ObjectLock.TurnInKey(this);
        }

        public ObjectLockKey(Core core, ObjectLock objectLock, ulong processId, LockOperation lockOperation)
        {
            _core = core;
            ProcessId = processId;
            ObjectLock = objectLock;
            LockOperation = lockOperation;
        }
    }
}
