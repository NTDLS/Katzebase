using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    public class LockIntention
    {
        public LockType LockType { get; private set; }
        public LockOperation Operation { get; private set; }
        public string DiskPath { get; private set; }

        public string Key => $"{LockType}:{Operation}:{DiskPath}";

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
            return (intention.LockType == LockType
                && intention.DiskPath == DiskPath);
        }

        public bool IsEqual(LockIntention intention)
        {
            return (intention.LockType == LockType
                && intention.Operation == Operation
                && intention.DiskPath == DiskPath);
        }
    }
}
