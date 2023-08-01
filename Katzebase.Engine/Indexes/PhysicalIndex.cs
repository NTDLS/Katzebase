using Katzebase.Engine.Library;
using Katzebase.Engine.Schemas;
using Newtonsoft.Json;
using System;
using System.Text;

namespace Katzebase.Engine.Indexes
{
    [Serializable]
    public class PhysicalIndex
    {
        public List<PhysicalIndexAttribute> Attributes { get; set; } = new List<PhysicalIndexAttribute>();
        public string Name { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modfied { get; set; }
        public int Partitions { get; set; } = 1000;
        public bool IsUnique { get; set; } = false;
        public string GetPartitionPagesPath(PhysicalSchema physicalSchema) => Path.Combine(physicalSchema.DiskPath, $"@Index_{Helpers.MakeSafeFileName(Name)}");
        public string GetPartitionPagesFileName(PhysicalSchema physicalSchema, int indexPartition) => Path.Combine(physicalSchema.DiskPath, $"@Index_{Helpers.MakeSafeFileName(Name)}", $"Page_{indexPartition}.PBuf");

        public PhysicalIndex()
        {
        }

        public int ComputePartition(string? value)
        {
            if (value == null)
            {
                return 0;
            }
            var buffer = Encoding.ASCII.GetBytes(value.ToLower());
            int sum = 0;
            foreach (var b in buffer)
            {
                sum += (int)(sum ^ b);
            }
            return sum % Partitions;
        }

        public PhysicalIndex Clone()
        {
            return new PhysicalIndex
            {
                Id = Id,
                Name = Name,
                Created = Created,
                Modfied = Modfied,
                IsUnique = IsUnique
            };
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

        static public PhysicalIndex FromClientPayload(PublicLibrary.Payloads.KbIndex index)
        {
            var persistIndex = new PhysicalIndex()
            {
                Id = index.Id,
                Name = index.Name,
                Created = index.Created,
                Modfied = index.Modfied,
                IsUnique = index.IsUnique
            };

            foreach (var indexAttribute in index.Attributes)
            {
                persistIndex.AddAttribute(PhysicalIndexAttribute.FromClientPayload(indexAttribute));
            }

            return persistIndex;
        }

        static public PublicLibrary.Payloads.KbIndex ToClientPayload(PhysicalIndex index)
        {
            var persistIndex = new PublicLibrary.Payloads.KbIndex()
            {
                Id = index.Id,
                Name = index.Name,
                Created = index.Created,
                Modfied = index.Modfied,
                IsUnique = index.IsUnique
            };

            foreach (var indexAttribute in index.Attributes)
            {
                persistIndex.AddAttribute(PhysicalIndexAttribute.ToClientPayload(indexAttribute));
            }

            return persistIndex;
        }
    }
}
