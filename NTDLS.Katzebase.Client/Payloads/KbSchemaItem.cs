namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbSchemaItem
    {
        public string? Name { get; set; }
        public Guid? Id { get; set; }
        public uint PageSize { get; set; }

        public KbSchemaItem Clone()
        {
            return new KbSchemaItem
            {
                Id = Id,
                Name = Name,
                PageSize = PageSize
            };
        }
    }
}
