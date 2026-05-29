using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.PersistentTypes.Atomicity;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    internal class ObjectLockIntention
    {
        public DateTime CreationTime { get; set; }
        public LockGranularity Granularity { get; private set; }
        public LockOperation Operation { get; private set; }
        public CacheKey TargetKey { get; private set; }

        public string Key => $"{Granularity}:{Operation}:{TargetKey}";

        public ObjectLockIntention(Transaction transaction, CacheKey targetKey, LockGranularity lockGranularity, LockOperation operation)
        {
            if (operation == LockOperation.Read && transaction.Session.GetConnectionSetting(StateSetting.ReadUncommitted, false))
            {
                operation = LockOperation.Stability;
            }

            CreationTime = DateTime.UtcNow;
            TargetKey = targetKey;
            Granularity = lockGranularity;
            Operation = operation;

            if ((lockGranularity == LockGranularity.Path
                || lockGranularity == LockGranularity.PathRecursive) && (TargetKey.Value.EndsWith('\\') == false))
            {
                TargetKey = new CacheKey($"{TargetKey.Value}\\");
            }
        }

        public string ObjectName
        {
            get
            {
                return $"{Granularity}:{TargetKey}";
            }
        }

        public bool IsObjectEqual(ObjectLockIntention intention)
        {
            return (intention.Granularity == Granularity
                && intention.TargetKey == TargetKey);
        }

        public bool IsEqual(ObjectLockIntention intention)
        {
            return (intention.Granularity == Granularity
                && intention.Operation == Operation
                && intention.TargetKey == TargetKey);
        }

        public new string ToString()
        {
            return $"{Granularity}+{Operation}:{TargetKey}";
        }
    }
}
