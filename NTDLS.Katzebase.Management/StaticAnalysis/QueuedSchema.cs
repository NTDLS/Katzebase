namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    /// <summary>
    /// Schemas that are to be queued for retrieval.
    /// </summary>
    internal class QueuedSchema
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ParentPath { get; set; }

        public QueuedSchema(Guid id, Guid parentId, string name, string path, string parentPath)
        {
            Id = id;
            ParentId = parentId;
            Name = name;
            Path = path;
            ParentPath = parentPath;
        }
    }
}
