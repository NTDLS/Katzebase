using System;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Locking
{
    public class ObjectLockKey
    {
        private Core core;
        public ObjectLock ObjectLock { get; set; }
        public LockOperation LockOperation { get; set; }
        public UInt64 ProcessId { get; set; }

        public void TurnInKey()
        {
            ObjectLock.TurnInKey(this);
        }

        public ObjectLockKey(Core core, ObjectLock objectLock, UInt64 processId, LockOperation lockOperation)
        {
            this.core = core;
            this.ProcessId = processId;
            this.ObjectLock = objectLock;
            this.LockOperation = lockOperation;
        }
    }
}
