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
        {
            Collection.Remove(item);
        }

        public void Add(PhysicalIndex item)
        {
            Collection.Add(item);
        }

        public PhysicalIndex? GetById(Guid id)
        {
            return (from o in Collection where o.Id == id select o).FirstOrDefault();
        }

        public PhysicalIndex? GetByName(string name)
        {
            foreach (var item in Collection)
            {
                if (item.Name.Is(name))
                {
                    return item;
                }
            }
            return null;
        }

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
