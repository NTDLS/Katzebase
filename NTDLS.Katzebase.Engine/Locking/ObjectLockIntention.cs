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

        /// <summary>
        /// Computed once: this is used as a dictionary key on every lock request.
        /// </summary>
        public string Key { get; private set; }

        public ObjectLockIntention(Transaction transaction, CacheKey targetKey, LockGranularity lockGranularity, LockOperation lockOp)
        {
            if (lockOp == LockOperation.Read && transaction.Session.GetConnectionSetting(StateSetting.ReadUncommitted, false))
            {
                lockOp = LockOperation.Stability;
            }

            CreationTime = DateTime.UtcNow;
            TargetKey = targetKey;
            Granularity = lockGranularity;
            Operation = lockOp;

            if ((lockGranularity == LockGranularity.Path
                || lockGranularity == LockGranularity.PathRecursive) && (TargetKey.Canonical.EndsWith(Path.DirectorySeparatorChar) == false))
            {
                TargetKey = new CacheKey(TargetKey.FilePath, $"{TargetKey.Canonical}{Path.DirectorySeparatorChar}");
            }

            Key = $"{Granularity}:{Operation}:{TargetKey}";
            DirectoryKey = (Path.GetDirectoryName(TargetKey.Canonical) ?? string.Empty) + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// The canonical directory that contains the target (for path locks, this is the path itself), ending with a directory separator.
        /// Used to find directory locks that cover this intention.
        /// </summary>
        public string DirectoryKey { get; private set; }

        public string ObjectName => $"{Granularity}:{TargetKey}";

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
