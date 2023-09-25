using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectLockKey
    {
        private readonly Core core;
        public ObjectLock ObjectLock { get; set; }
        public LockOperation LockOperation { get; set; }
        public ulong ProcessId { get; set; }

        public void TurnInKey()
        {
            ObjectLock.TurnInKey(this);
        }

        public ObjectLockKey(Core core, ObjectLock objectLock, ulong processId, LockOperation lockOperation)
        {
            this.core = core;
            this.ProcessId = processId;
            this.ObjectLock = objectLock;
            this.LockOperation = lockOperation;
        }
    }
}
