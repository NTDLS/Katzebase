using Katzebase.Engine.Documents;
using Katzebase.Engine.Library;
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

        [JsonIgnore]
        public bool IsTemporary { get; set; }

        public string IndexCatalogFilePath()
            => Path.Combine(DiskPath, EngineConstants.IndexCatalogFile);

        public string DocumentPageCatalogFilePath()
            => Path.Combine(DiskPath, EngineConstants.DocumentPageCatalogFile);

        public string SchemaCatalogFilePath()
            => Path.Combine(DiskPath, EngineConstants.SchemaCatalogFile);

        public string DocumentPageCatalogItemFilePath(DocumentPointer documentPointer)
            => Path.Combine(DiskPath, $"{documentPointer.PageNumber}{EngineConstants.DocumentPageExtension}");

        public string DocumentPageCatalogItemDiskPath(PhysicalDocumentPageMap documentPageCatalogItem)
            => Path.Combine(DiskPath, $"{documentPageCatalogItem.PageNumber}{EngineConstants.DocumentPageExtension}");



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

        public KbSchemaItem ToClientPayload()
        {
            return new KbSchemaItem()
            {
                Id = Id,
                Name = Name
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
