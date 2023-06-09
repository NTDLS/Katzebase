using static Katzebase.Engine.KbLib.EngineConstants;

namespace Katzebase.Engine.Locking
{
    public class LockIntention
    {
        public LockType LockType { get; private set; }
        public LockOperation Operation { get; private set; }
        public string DiskPath { get; private set; }

        public LockIntention(string diskPath, LockType lockType, LockOperation operation)
        {
            DiskPath = diskPath;
            LockType = lockType;
            Operation = operation;

            if (lockType == LockType.Directory && (DiskPath.EndsWith('\\') == false))
            {
                DiskPath = $"{DiskPath}\\";
            }
        }

        public string ObjectName
        {
            get
            {
                return $"{LockType}:{DiskPath}";
            }
        }

        public bool IsObjectEqual(LockIntention intention)
        {
            return (intention.LockType == this.LockType
                && intention.DiskPath == this.DiskPath);
        }

        public bool IsEqual(LockIntention intention)
        {
            return (intention.LockType == this.LockType
                && intention.Operation == this.Operation
                && intention.DiskPath == this.DiskPath);
        }
    }
}
