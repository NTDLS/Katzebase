namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbSchemaItem
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? ParentPath { get; set; }
        public uint PageSize { get; set; }

        public KbSchemaItem Clone()
        {
            return new KbSchemaItem
            {
                Id = Id,
                Name = Name,
                Path = Path,
                ParentPath = ParentPath,
                PageSize = PageSize
            };
        }
    }
}
