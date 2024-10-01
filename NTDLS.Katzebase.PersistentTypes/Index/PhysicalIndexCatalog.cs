using Newtonsoft.Json;
using NTDLS.Helpers;

namespace NTDLS.Katzebase.PersistentTypes.Index
{
    [Serializable]
    public class PhysicalIndexCatalog
    {
        public List<PhysicalIndex> Collection = new();

        [JsonIgnore]
        public string? DiskPath { get; set; }

        public void Remove(PhysicalIndex item)
            => Collection.Remove(item);

        public void Add(PhysicalIndex item)
            => Collection.Add(item);

        public PhysicalIndex? GetById(Guid id)
            => Collection.FirstOrDefault(o => o.Id == id);

        public PhysicalIndex? GetByName(string name)
            => Collection.FirstOrDefault(o => o.Name.Is(name));

        public PhysicalIndexCatalog Clone()
        {
            var catalog = new PhysicalIndexCatalog();

            foreach (var obj in Collection)
            {
                catalog.Collection.Add(obj.Clone());
            }

            return catalog;
        }
    }
}
