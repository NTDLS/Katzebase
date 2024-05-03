using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    public class ObjectLockIntention
    {
        public DateTime CreationTime { get; set; }
        public LockGranularity Granularity { get; private set; }
        public LockOperation Operation { get; private set; }
        public string DiskPath { get; private set; }

        public string Key => $"{Granularity}:{Operation}:{DiskPath}";

        public ObjectLockIntention(string diskPath, LockGranularity lockGranularity, LockOperation operation)
        {
            CreationTime = DateTime.UtcNow;
            DiskPath = diskPath;
            Granularity = lockGranularity;
            Operation = operation;

            if ((lockGranularity == LockGranularity.Directory || lockGranularity == LockGranularity.RecursiveDirectory) && (DiskPath.EndsWith('\\') == false))
            {
                DiskPath = $"{DiskPath}\\";
            }
        }

        public string ObjectName
        {
            get
            {
                return $"{Granularity}:{DiskPath}";
            }
        }

        public bool IsObjectEqual(ObjectLockIntention intention)
        {
            return (intention.Granularity == Granularity
                && intention.DiskPath == DiskPath);
        }

        public bool IsEqual(ObjectLockIntention intention)
        {
            return (intention.Granularity == Granularity
                && intention.Operation == Operation
                && intention.DiskPath == DiskPath);
        }

        public new string ToString()
        {
            return $"{Granularity}+{Operation}:{DiskPath}";
        }
    }
}
