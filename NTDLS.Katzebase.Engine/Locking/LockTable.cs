using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Locking
{
    /// <summary>
    /// The set of all granted locks across all transactions, indexed by granularity and target so that
    /// finding the locks that overlap a lock intention does not require a scan of every lock in the engine.
    ///
    /// Not thread-safe, access is protected by the LockManager's critical resource.
    /// </summary>
    internal class LockTable
    {
        /// <summary>
        /// Object locks, keyed by the canonical key of the object.
        /// </summary>
        private readonly Dictionary<string, ObjectLock> _objectLocks = new(StringComparer.Ordinal);

        /// <summary>
        /// Path (non-recursive) locks, keyed by the canonical path, which always ends with a directory separator.
        /// </summary>
        private readonly Dictionary<string, ObjectLock> _pathLocks = new(StringComparer.Ordinal);

        /// <summary>
        /// Recursive path locks. These are rare (schema drops, etc.) so a list is fine.
        /// </summary>
        private readonly List<ObjectLock> _recursivePathLocks = new();

        public int Count => _objectLocks.Count + _pathLocks.Count + _recursivePathLocks.Count;

        public void Add(ObjectLock objectLock)
        {
            switch (objectLock.Granularity)
            {
                case LockGranularity.Object:
                    _objectLocks[objectLock.TargetKey.Canonical] = objectLock;
                    break;
                case LockGranularity.Path:
                    _pathLocks[objectLock.TargetKey.Canonical] = objectLock;
                    break;
                case LockGranularity.PathRecursive:
                    _recursivePathLocks.Add(objectLock);
                    break;
                default:
                    throw new NotImplementedException($"Lock granularity is not implemented: [{objectLock.Granularity}].");
            }
        }

        public void Remove(ObjectLock objectLock)
        {
            switch (objectLock.Granularity)
            {
                case LockGranularity.Object:
                    RemoveIfSame(_objectLocks, objectLock);
                    break;
                case LockGranularity.Path:
                    RemoveIfSame(_pathLocks, objectLock);
                    break;
                case LockGranularity.PathRecursive:
                    _recursivePathLocks.Remove(objectLock);
                    break;
            }

            static void RemoveIfSame(Dictionary<string, ObjectLock> collection, ObjectLock objectLock)
            {
                if (collection.TryGetValue(objectLock.TargetKey.Canonical, out var existing) && ReferenceEquals(existing, objectLock))
                {
                    collection.Remove(objectLock.TargetKey.Canonical);
                }
            }
        }

        /// <summary>
        /// Returns the set (if any) of existing locks that overlap the given lock intention.
        /// </summary>
        public HashSet<ObjectLock> GetOverlappingLocks(ObjectLockIntention intention)
        {
            var result = new HashSet<ObjectLock>();

            var intentionDirectory = intention.DirectoryKey;

            //If we are locking a file, then look for all other locks for the exact path.
            if (intention.Granularity == LockGranularity.Object
                && _objectLocks.TryGetValue(intention.TargetKey.Canonical, out var objectLock))
            {
                result.Add(objectLock);
            }

            //Check if the intended file or directory is in a locked directory.
            if (_pathLocks.TryGetValue(intentionDirectory, out var pathLock))
            {
                result.Add(pathLock);
            }

            //Check if the intended file or directory is beneath a recursively locked directory.
            foreach (var recursiveLock in _recursivePathLocks)
            {
                if (intentionDirectory.StartsWith(recursiveLock.TargetKey.Canonical, StringComparison.Ordinal))
                {
                    result.Add(recursiveLock);
                }
            }

            // A PathRecursive intention must also see finer-grained locks that already
            // exist within its target subtree, otherwise a schema-level delete could be
            // granted while another transaction holds an Object or Path lock on a child.
            if (intention.Granularity == LockGranularity.PathRecursive)
            {
                foreach (var childLock in _objectLocks.Values.Concat(_pathLocks.Values))
                {
                    if (childLock.TargetKey.Canonical.StartsWith(intention.TargetKey.Canonical, StringComparison.Ordinal))
                    {
                        result.Add(childLock);
                    }
                }
            }

            return result;
        }
    }
}
