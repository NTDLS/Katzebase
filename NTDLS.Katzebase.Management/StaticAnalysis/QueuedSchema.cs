namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    /// <summary>
    /// Schemas that are to be queued for retrevial.
    /// </summary>
    internal class QueuedSchema
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ParentPath { get; set; }

        public QueuedSchema(Guid id, string name, string path, string parentPath)
        {
            Id = id;
            Name = name;
            Path = path;
            ParentPath = parentPath;
        }
    }
}
