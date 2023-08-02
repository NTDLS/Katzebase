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

        public uint PageSize { get; set; }

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

        public string ProcedureCatalogFilePath()
            => Path.Combine(DiskPath, EngineConstants.ProcedureCatalogFile);

        public string DocumentPageCatalogFilePath()
            => Path.Combine(DiskPath, EngineConstants.DocumentPageCatalogFile);

        public string SchemaCatalogFilePath()
            => Path.Combine(DiskPath, EngineConstants.SchemaCatalogFile);

        public string DocumentPageCatalogItemFilePath(int pageNumber)
            => Path.Combine(DiskPath, $"{pageNumber}{EngineConstants.DocumentPageExtension}");

        public string DocumentPageCatalogItemFilePath(DocumentPointer documentPointer)
            => DocumentPageCatalogItemFilePath(documentPointer.PageNumber);

        public string DocumentPageCatalogItemDiskPath(PhysicalDocumentPageCatalogItem documentPageCatalogItem)
            => DocumentPageCatalogItemDiskPath(documentPageCatalogItem.PageNumber);

        public string DocumentPageCatalogItemDiskPath(int pageNumber)
            => Path.Combine(DiskPath, $"{pageNumber}{EngineConstants.DocumentPageExtension}");

        public string PhysicalDocumentPageMapFilePath(int pageNumber)
            => Path.Combine(DiskPath, $"{pageNumber}{EngineConstants.DocumentPageDocumentIdExtension}");

        public string PhysicalDocumentPageMapFilePath(DocumentPointer documentPointer)
            => PhysicalDocumentPageMapFilePath(documentPointer.PageNumber);

        public string PhysicalDocumentPageMapFilePath(PhysicalDocumentPageCatalogItem pageCatalogItem)
            => PhysicalDocumentPageMapFilePath(pageCatalogItem.PageNumber);

        public PhysicalSchema Clone()
        {
            return new PhysicalSchema
            {
                DiskPath = DiskPath,
                Id = Id,
                Name = Name,
                VirtualPath = VirtualPath,
                PageSize = PageSize,
                IsTemporary = IsTemporary,
            };
        }

        public KbSchemaItem ToClientPayload()
        {
            return new KbSchemaItem()
            {
                Id = Id,
                Name = Name,
                PageSize = PageSize                 
            };
        }

        public VirtualSchema ToVirtual()
        {
            return new VirtualSchema
            {
                DiskPath = DiskPath,
                Id = Id,
                Name = Name,
                VirtualPath = VirtualPath,
                PageSize = PageSize,
                IsTemporary = IsTemporary,
            };
        }
    }
}
