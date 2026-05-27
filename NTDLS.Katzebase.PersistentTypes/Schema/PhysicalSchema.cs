using Newtonsoft.Json;
using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.PersistentTypes.Schema
{
    public class PhysicalSchema
    {
        /// <summary>
        /// VirtualSchema is used in the cases where we need to lock a schema that may not exist yet.
        /// </summary>
        public class VirtualSchema
            : PhysicalSchema
        {
            [JsonIgnore]
            public bool Exists { get; set; }

            [JsonIgnore]
            public PhysicalSchema ParentPhysicalSchema { get; set; }

            public VirtualSchema(PhysicalSchema parentPhysicalSchema)
                => ParentPhysicalSchema = parentPhysicalSchema;
        }

        public string Name { get; set; } = string.Empty;
        public Guid Id { get; set; }

        [JsonIgnore]
        public string DiskPath { get; set; } = string.Empty;

        [JsonIgnore]
        public string VirtualPath { get; set; } = string.Empty;

        [JsonIgnore]
        public bool IsTemporary { get; set; }

        public string ProcedureCatalogFilePath()
            => Path.Combine(DiskPath, EngineConstants.ProcedureCatalogFile);

        public string DocumentsFilePath()
            => Path.Combine(DiskPath, EngineConstants.DocumentsFile);

        public string SchemaFilePath()
            => Path.Combine(DiskPath, EngineConstants.SchemaFile);

        public PhysicalSchema Clone()
        {
            return new PhysicalSchema
            {
                DiskPath = DiskPath,
                Id = Id,
                Name = Name,
                VirtualPath = VirtualPath,
                IsTemporary = IsTemporary,
            };
        }

        public KbSchema ToClientPayload(Guid parentSchemaId, string parentPath)
            => new(Id, Name, $"{parentPath.TrimEnd(':')}:{Name}".Trim(':'), parentPath.Trim(':'), parentSchemaId);

        public VirtualSchema ToVirtual(PhysicalSchema parentPhysicalSchema)
            => new(parentPhysicalSchema)
            {
                DiskPath = DiskPath,
                Id = Id,
                Name = Name,
                VirtualPath = VirtualPath,
                IsTemporary = IsTemporary,
            };

    }
}
