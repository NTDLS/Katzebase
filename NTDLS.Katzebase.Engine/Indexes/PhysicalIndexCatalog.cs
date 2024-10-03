using Newtonsoft.Json;
using NTDLS.Katzebase.Shared;
using NTDLS.Katzebase.Parsers.Interfaces;
using NTDLS.Helpers;
namespace NTDLS.Katzebase.Engine.Indexes
{
    [Serializable]
    public class PhysicalIndexCatalog<TData> where TData:IStringable
    {
        public List<PhysicalIndex<TData>> Collection = new();

        [JsonIgnore]
        public string? DiskPath { get; set; }

        public void Remove(PhysicalIndex<TData> item)
        {
            Collection.Remove(item);
        }

        public void Add(PhysicalIndex<TData> item)
        {
            Collection.Add(item);
        }

        public PhysicalIndex<TData>? GetById(Guid id)
        {
            return (from o in Collection where o.Id == id select o).FirstOrDefault();
        }

        public PhysicalIndex<TData>? GetByName(string name)
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

        public PhysicalIndexCatalog<TData> Clone()
        {
            var catalog = new PhysicalIndexCatalog<TData>();

            foreach (var obj in Collection)
            {
                catalog.Collection.Add(obj.Clone());
            }

            return catalog;
        }
    }
}
