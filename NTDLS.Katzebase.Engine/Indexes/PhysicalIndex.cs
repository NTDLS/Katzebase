using NTDLS.Katzebase.Engine.Library;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Engine.Indexes
{
    [Serializable]
    public class PhysicalIndex<TData> : IPhysicalIndex<TData> where TData : IStringable
    {
        public List<IPhysicalIndexAttribute> Attributes { get; set; } = new List<IPhysicalIndexAttribute>();
        public string Name { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public uint Partitions { get; set; } = 1000;
        public bool IsUnique { get; set; } = false;
        public string GetPartitionPagesPath(IPhysicalSchema physicalSchema)
            => Path.Combine(physicalSchema.DiskPath, $"@Index_{Library.Helpers.MakeSafeFileName(Name)}");

        public string GetPartitionPagesFileName(IPhysicalSchema physicalSchema, uint indexPartition)
            => Path.Combine(physicalSchema.DiskPath, $"@Index_{Library.Helpers.MakeSafeFileName(Name)}", $"Page_{indexPartition}{EngineConstants.IndexPageExtension}");

        public PhysicalIndex()
        {
        }

        public uint ComputePartition(TData? value)
        {
            uint hash = 0;
            if (value == null ? true : value.IsNullOrEmpty())
                return hash;
            value = (TData)value.ToLowerInvariant();
            const uint seed = 131;
            foreach (char c in value.ToT<char[]>())
            {
                hash = hash * seed + c;
            }
            return hash % Partitions;
        }

        public PhysicalIndex<TData> Clone()
        {
            var result = new PhysicalIndex<TData>
            {
                Id = Id,
                Name = Name,
                Created = Created,
                Modified = Modified,
                IsUnique = IsUnique,
                Partitions = Partitions,
            };

            foreach (var attribute in Attributes)
            {
                result.Attributes.Add(attribute.Clone());
            }

            return result;
        }

        public void AddAttribute(string name)
        {
            AddAttribute(new PhysicalIndexAttribute()
            {
                Field = name
            });
        }

        public void AddAttribute(PhysicalIndexAttribute attribute)
        {
            Attributes.Add(attribute);
        }

        static public PhysicalIndex<TData> FromClientPayload(Client.Payloads.KbIndex index)
        {
            var persistIndex = new PhysicalIndex<TData>()
            {
                Id = index.Id,
                Name = index.Name,
                Created = index.Created,
                Modified = index.Modified,
                IsUnique = index.IsUnique,
                Partitions = index.Partitions
            };

            foreach (var attribute in index.Attributes)
            {
                persistIndex.AddAttribute(PhysicalIndexAttribute.FromClientPayload(attribute));
            }

            return persistIndex;
        }

        static public Client.Payloads.KbIndex ToClientPayload(PhysicalIndex<TData> index)
        {
            var persistIndex = new Client.Payloads.KbIndex()
            {
                Id = index.Id,
                Name = index.Name,
                Created = index.Created,
                Modified = index.Modified,
                IsUnique = index.IsUnique,
                Partitions = index.Partitions
            };

            foreach (var attribute in index.Attributes)
            {
                persistIndex.AddAttribute(PhysicalIndexAttribute.ToClientPayload(attribute));
            }

            return persistIndex;
        }
    }
}
