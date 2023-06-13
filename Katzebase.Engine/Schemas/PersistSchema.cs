using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json;

namespace Katzebase.Engine.Schemas
{
    public class PersistSchema
    {
        public string? Name { get; set; }
        public Guid Id { get; set; }

        [JsonIgnore]
        public string? DiskPath { get; set; }
        [JsonIgnore]
        public string? VirtualPath { get; set; }
        [JsonIgnore]
        public bool Exists { get; set; }

        static public KbSchemaItem ToPayload(PersistSchema schema)
        {
            return new KbSchemaItem()
            {
                Id = schema.Id,
                Name = schema.Name
            };
        }

        public PersistSchema Clone()
        {
            return new PersistSchema
            {
                DiskPath = DiskPath,
                Exists = Exists,
                Id = Id,
                Name = Name,
                VirtualPath = VirtualPath
            };
        }
    }
}
