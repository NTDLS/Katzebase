using Katzebase.Engine.Documents;
using Katzebase.Engine.KbLib;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Client.Management;
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

        public string DocumentPageCatalogDiskPath()
        {
            Utility.EnsureNotNull(DiskPath);
            return Path.Combine(DiskPath, EngineConstants.DocumentPageCatalogFile);
        }

        public string DocumentPageCatalogItemDiskPath(PersistDocumentPageCatalogItem documentPageCatalogItem)
        {
            Utility.EnsureNotNull(DiskPath);
            return Path.Combine(DiskPath, $"{documentPageCatalogItem.PageNumber}{EngineConstants.DocumentPageExtension}");
        }

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
