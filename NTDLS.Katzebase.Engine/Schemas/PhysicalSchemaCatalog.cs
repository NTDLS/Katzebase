using NTDLS.Helpers;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Schemas
{
    public class PhysicalSchemaCatalog<TData> where TData : IStringable
    {
        public List<PhysicalSchema<TData>> Collection = new();

        public void Add(PhysicalSchema<TData> schema)
        {
            Collection.Add(schema);
        }

        public bool ContainsName(string name)
        {
            foreach (var item in Collection)
            {
                if (item.Name.Is(name))
                {
                    return true;
                }
            }
            return false;
        }

        public PhysicalSchema<TData>? GetByName(string name)
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

        public PhysicalSchema<TData>? GetById(Guid id)
        {
            return Collection.FirstOrDefault(o => o.Id == id);
        }

        public PhysicalSchemaCatalog<TData> Clone()
        {
            var catalog = new PhysicalSchemaCatalog<TData>();

            lock (this)
            {
                foreach (var obj in Collection)
                {
                    catalog.Collection.Add(obj.Clone());
                }
            }

            return catalog;
        }

    }
}
