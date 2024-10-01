namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    /// <summary>
    /// Schemas that are cached for static analysis
    /// </summary>
    public class CachedSchema
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ParentPath { get; set; }

        public List<string>? Fields => throw new NotImplementedException();
        public List<string>? Indexes => throw new NotImplementedException();

        public CachedSchema(Guid id, Guid parentId, string name, string path, string parentPath)
        {
            Id = id;
            ParentId = parentId;
            Name = name;
            Path = path;
            ParentPath = parentPath;
        }
    }
}
