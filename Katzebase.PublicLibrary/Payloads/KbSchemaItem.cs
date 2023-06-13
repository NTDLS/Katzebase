namespace Katzebase.PublicLibrary.Payloads
{
    public class KbSchemaItem
    {
        public string? Name { get; set; }
        public Guid? Id { get; set; }

        public KbSchemaItem Clone()
        {
            return new KbSchemaItem
            {
                Id = this.Id,
                Name = this.Name
            };
        }
    }
}
