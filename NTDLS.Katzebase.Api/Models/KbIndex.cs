﻿namespace NTDLS.Katzebase.Api.Models
{
    public class KbIndex
    {
        public List<KbIndexAttribute> Attributes { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public virtual bool IsUnique { get; set; }
        public uint Partitions { get; set; }

        public KbIndex(string name)
        {
            Name = name;
        }

        public KbIndex()
        {
        }

        public override int GetHashCode()
        {
            int hash = HashCode.Combine(
                Name,
                Id,
                Created,
                Modified,
                IsUnique,
                Partitions
            );

            foreach (var attribute in Attributes)
            {
                hash = HashCode.Combine(hash, attribute.GetHashCode());
            }

            return hash;
        }

        public KbIndex(string name, string[] attributes)
        {
            Name = name;
            foreach (var attribute in attributes)
            {
                AddAttribute(attribute);
            }
        }

        public KbIndex(string name, string attributesCsv)
        {
            Name = name;
            foreach (var attribute in attributesCsv.Split(","))
            {
                AddAttribute(attribute);
            }
        }

        public void AddAttribute(string name)
        {
            Attributes.Add(new KbIndexAttribute()
            {
                Field = name
            });
        }

        public void AddAttribute(KbIndexAttribute attribute)
        {
            Attributes.Add(attribute);
        }
    }
}
