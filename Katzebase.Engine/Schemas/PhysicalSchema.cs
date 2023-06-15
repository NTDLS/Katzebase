using Katzebase.Engine.Documents;
using Katzebase.Engine.KbLib;
using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json;

namespace Katzebase.Engine.Schemas
{
    public class PhysicalSchema
    {
        /// <summary>
        /// VirtualSchema is used in the cases where we need to lock a schema that may not exist yet.
        /// </summary>
        public class VirtualSchema : PhysicalSchema
        {
            [JsonIgnore]
            public bool Exists { get; set; }
        }

        public string Name { get; set; } = string.Empty;
        public Guid Id { get; set; }

        [JsonIgnore]
        public string DiskPath { get; set; } = string.Empty;
        [JsonIgnore]
        public string VirtualPath { get; set; } = string.Empty;

        public string DocumentPageCatalogDiskPath()
        {
            return Path.Combine(DiskPath, EngineConstants.DocumentPageCatalogFile);
        }

        public string DocumentPageCatalogItemDiskPath(PageDocument pageDocument)
        {
            return Path.Combine(DiskPath, $"{pageDocument.PageNumber}{EngineConstants.DocumentPageExtension}");
        }

        public string DocumentPageCatalogItemDiskPath(PhysicalDocumentPageMap documentPageCatalogItem)
        {
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
                Id = Id,
                Name = Name,
                VirtualPath = VirtualPath
            };
        }

        public VirtualSchema ToVirtual()
        {
            return new VirtualSchema
            {
                DiskPath = DiskPath,
                Id = Id,
                Name = Name,
                VirtualPath = VirtualPath
            };
        }

    }
}
