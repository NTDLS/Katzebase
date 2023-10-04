using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    public class LockIntention
    {
        public LockGranularity Granularity { get; private set; }
        public LockOperation Operation { get; private set; }
        public string DiskPath { get; private set; }

        public string Key => $"{Granularity}:{Operation}:{DiskPath}";

        public LockIntention(string diskPath, LockGranularity lockGranularity, LockOperation operation)
        {
            DiskPath = diskPath;
            Granularity = lockGranularity;
            Operation = operation;

            if (lockGranularity == LockGranularity.Directory && (DiskPath.EndsWith('\\') == false))
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

        public bool IsObjectEqual(LockIntention intention)
        {
            return (intention.Granularity == Granularity
                && intention.DiskPath == DiskPath);
        }

        public bool IsEqual(LockIntention intention)
        {
            return (intention.Granularity == Granularity
                && intention.Operation == Operation
                && intention.DiskPath == DiskPath);
        }

        public new string ToString()
        {
            return $"{Granularity}+{Operation}->{DiskPath}";
        }
    }
}
