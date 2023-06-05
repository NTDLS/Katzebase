using Newtonsoft.Json;

namespace Katzebase.Engine.Indexes
{
    [Serializable]
    public class PersistIndex
    {
        public List<PersistIndexAttribute> Attributes { get; set; } = new List<PersistIndexAttribute>();
        public string Name { get; set; } = string.Empty;
        public Guid? Id { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Modfied { get; set; }
        public bool IsUnique { get; set; } = false;

        [JsonIgnore]
        public string? DiskPath { get; set; }

        public PersistIndex()
        {
        }

        public PersistIndex Clone()
        {
            return new PersistIndex
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
            AddAttribute(new PersistIndexAttribute()
            {
                Field = name
            });
        }
        public void AddAttribute(PersistIndexAttribute attribute)
        {
            Attributes.Add(attribute);
        }

        static public PersistIndex FromPayload(PublicLibrary.Payloads.KbIndex index)
        {
            var persistIndex = new PersistIndex()
            {
                Id = index.Id,
                Name = index.Name,
                Created = index.Created,
                Modfied = index.Modfied,
                IsUnique = index.IsUnique
            };

            foreach (var indexAttribute in index.Attributes)
            {
                persistIndex.AddAttribute(PersistIndexAttribute.FromPayload(indexAttribute));
            }

            return persistIndex;
        }
    }
}
