using Newtonsoft.Json;
using System;
using Katzebase.Library.Payloads;

namespace Katzebase.Engine.Schemas
{
    public class PersistSchema
    {
        public string Name { get; set; }
        public Guid Id { get; set; }

        [JsonIgnore]
        public string DiskPath { get; set; }
        [JsonIgnore]
        public string VirtualPath { get; set; }
        [JsonIgnore]
        public bool Exists { get; set; }

        public Schema ToPayload()
        {
            return new Schema()
            {
                Id = Id,
                Name = Name
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
