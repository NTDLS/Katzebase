namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbSchema
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ParentPath { get; set; }
        public Guid ParentId { get; set; }
        public uint PageSize { get; set; }

        public KbSchema(Guid id, string name, string path, string parentPath, Guid parentId, uint pageSize)
        {
            Id = id;
            Name = name;
            Path = path;
            ParentPath = parentPath;
            ParentId = parentId;
            PageSize = pageSize;
        }

        public KbSchema Clone()
            => new KbSchema(Id, Name, Path, ParentPath, ParentId, PageSize);
    }
}
