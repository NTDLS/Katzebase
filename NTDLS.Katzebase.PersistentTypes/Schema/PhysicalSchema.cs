﻿using Newtonsoft.Json;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.PersistentTypes.Document;
using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.PersistentTypes.Schema
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

            [JsonIgnore]
            public PhysicalSchema ParentPhysicalSchema { get; set; }

            public VirtualSchema(PhysicalSchema parentPhysicalSchema)
                => ParentPhysicalSchema = parentPhysicalSchema;
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

        public string PolicyCatalogFileFilePath()
            => Path.Combine(DiskPath, EngineConstants.PolicyCatalogFile);

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

        public KbSchema ToClientPayload(Guid parentSchemaId, string parentPath)
            => new(Id, Name, $"{parentPath.TrimEnd(':')}:{Name}".Trim(':'), parentPath.Trim(':'), parentSchemaId, PageSize);

        public VirtualSchema ToVirtual(PhysicalSchema parentPhysicalSchema)
            => new(parentPhysicalSchema)
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
