namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    /// <summary>
    /// An immutable, point-in-time view of the schema cache. Published by LazyBackgroundSchemaCache whenever the cache
    /// changes; readers (static analysis, UI) can use it from any thread without locking.
    /// </summary>
    public class SchemaCacheSnapshot
    {
        public static SchemaCacheSnapshot Empty { get; } = new(new Dictionary<string, CachedSchema>(StringComparer.InvariantCultureIgnoreCase), 0);

        private readonly Dictionary<string, CachedSchema> _byPath;
        private readonly Dictionary<Guid, CachedSchema> _byId;

        /// <summary>
        /// Incremented every time the cache changes.
        /// </summary>
        public long Version { get; }

        public IReadOnlyCollection<CachedSchema> Schemas => _byPath.Values;

        /// <param name="byPath">Must use a case-insensitive comparer. Ownership is taken, the caller must not modify it afterwards.</param>
        internal SchemaCacheSnapshot(Dictionary<string, CachedSchema> byPath, long version)
        {
            _byPath = byPath;
            _byId = new Dictionary<Guid, CachedSchema>(byPath.Count);
            foreach (var item in byPath.Values)
            {
                _byId[item.Schema.Id] = item;
            }
            Version = version;
        }

        public bool TryGet(string schemaPath, out CachedSchema cachedSchema)
            => _byPath.TryGetValue(NormalizePath(schemaPath), out cachedSchema!);

        public bool TryGet(Guid schemaId, out CachedSchema cachedSchema)
            => _byId.TryGetValue(schemaId, out cachedSchema!);

        /// <summary>
        /// Determines whether a schema exists on the server, as far as the cache knows:
        ///   true:  the schema is cached.
        ///   false: the schema's nearest cached ancestor has had its children listed and the schema was not among them.
        ///   null:  unknown, the relevant part of the schema tree has not been loaded yet.
        /// Distinguishing "unknown" prevents static analysis from reporting schemas as missing before they have been discovered.
        /// </summary>
        public bool? Exists(string schemaPath)
        {
            var path = NormalizePath(schemaPath);
            if (_byPath.ContainsKey(path))
            {
                return true;
            }

            var ancestorPath = ParentPathOf(path);
            while (true)
            {
                if (_byPath.TryGetValue(ancestorPath, out var ancestor))
                {
                    return ancestor.ChildrenLoadedUtc != null ? false : null;
                }

                if (ancestorPath.Length == 0)
                {
                    return null;
                }
                ancestorPath = ParentPathOf(ancestorPath);
            }
        }

        /// <summary>
        /// Schema paths are case-insensitive and colon-delimited, the root schema's path is an empty string.
        /// </summary>
        public static string NormalizePath(string? schemaPath)
            => (schemaPath ?? string.Empty).Trim().Trim(':').Trim();

        public static string ParentPathOf(string schemaPath)
        {
            var path = NormalizePath(schemaPath);
            var lastSeparator = path.LastIndexOf(':');
            return lastSeparator < 0 ? string.Empty : path[..lastSeparator];
        }

        /// <summary>
        /// Returns true if [schemaPath] is [ancestorPath] or is beneath it.
        /// </summary>
        public static bool IsSameOrDescendant(string schemaPath, string ancestorPath)
        {
            var path = NormalizePath(schemaPath);
            var ancestor = NormalizePath(ancestorPath);

            return ancestor.Length == 0
                || path.Equals(ancestor, StringComparison.InvariantCultureIgnoreCase)
                || path.StartsWith(ancestor + ":", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
