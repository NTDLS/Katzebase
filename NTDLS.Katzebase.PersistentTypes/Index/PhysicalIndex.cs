using NTDLS.Katzebase.Api.Models;

namespace NTDLS.Katzebase.PersistentTypes.Index
{
    [Serializable]
    public class PhysicalIndex
    {
        public List<PhysicalIndexAttribute> Attributes { get; set; } = new List<PhysicalIndexAttribute>();
        public string Name { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public bool IsUnique { get; set; } = false;

        public PhysicalIndex()
        {
        }

        public PhysicalIndex Clone()
        {
            var result = new PhysicalIndex
            {
                Id = Id,
                Name = Name,
                Created = Created,
                Modified = Modified,
                IsUnique = IsUnique
            };

            foreach (var attribute in Attributes)
            {
                result.AddAttribute(attribute.Clone());
            }

            return result;
        }

        public void AddAttribute(string name)
        {
            Attributes.Add(new PhysicalIndexAttribute()
            {
                Field = name
            });
        }

        public void AddAttribute(PhysicalIndexAttribute attribute)
            => Attributes.Add(attribute);

        static public PhysicalIndex FromClientPayload(KbIndex index)
        {
            var persistIndex = new PhysicalIndex()
            {
                Id = index.Id,
                Name = index.Name,
                Created = index.Created,
                Modified = index.Modified,
                IsUnique = index.IsUnique
            };

            foreach (var attribute in index.Attributes)
            {
                persistIndex.AddAttribute(PhysicalIndexAttribute.FromClientPayload(attribute));
            }

            return persistIndex;
        }

        static public KbIndex? ToApiPayload(PhysicalIndex? index)
        {
            if (index == null)
            {
                return null;
            }

            var apiResult = new KbIndex()
            {
                Id = index.Id,
                Name = index.Name,
                Created = index.Created,
                Modified = index.Modified,
                IsUnique = index.IsUnique
            };

            foreach (var attribute in index.Attributes)
            {
                apiResult.AddAttribute(PhysicalIndexAttribute.ToClientPayload(attribute));
            }

            return apiResult;
        }
    }
}
