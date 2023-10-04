using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectLockKey
    {
        public ObjectLock ObjectLock { get; private set; }
        public LockOperation Operation { get; private set; }
        public ulong ProcessId { get; private set; }

        public void TurnInKey()
        {
            ObjectLock.TurnInKey(this);
        }

        public ObjectLockKey(ObjectLock objectLock, ulong processId, LockOperation operation)
        {
            ProcessId = processId;
            ObjectLock = objectLock;
            Operation = operation;
        }

        public new string ToString()
        {
            return $"{ObjectLock.Granularity}+{Operation}->{ObjectLock.DiskPath}";
        }
    }
}
