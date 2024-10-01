namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    /// <summary>
    /// Schemas that are cached for static analysis
    /// </summary>
    internal class CachedSchema
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ParentPath { get; set; }

        /// <summary>
        /// Cached fields are not implemented.
        /// </summary>
        public List<string>? Fields { get; set; }

        public CachedSchema(Guid id, string name, string path, string parentPath)
        {
            Id = id;
            Name = name;
            Path = path;
            ParentPath = parentPath;
        }
    }
}
