using Katzebase.Engine.Documents;
using Katzebase.Engine.KbLib;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Client.Management;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json;

namespace Katzebase.Engine.Schemas
{
    public class PhysicalSchema
    {
        public string? Name { get; set; }
        public Guid Id { get; set; }

        [JsonIgnore]
        public string? DiskPath { get; set; }
        [JsonIgnore]
        public string? VirtualPath { get; set; }
        [JsonIgnore]
        public bool Exists { get; set; }

        public string DocumentPageCatalogDiskPath()
        {
            Utility.EnsureNotNull(DiskPath);
            return Path.Combine(DiskPath, EngineConstants.DocumentPageCatalogFile);
        }

        public string DocumentPageCatalogItemDiskPath(PhysicalDocumentPageCatalogItem documentPageCatalogItem)
        {
            Utility.EnsureNotNull(DiskPath);
            return Path.Combine(DiskPath, $"{documentPageCatalogItem.PageNumber}{EngineConstants.DocumentPageExtension}");
        }

        static public KbSchemaItem ToPayload(PhysicalSchema schema)
        {
            return new KbSchemaItem()
            {
                Id = schema.Id,
                Name = schema.Name
            };
        }

        public PhysicalSchema Clone()
        {
            return new PhysicalSchema
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
