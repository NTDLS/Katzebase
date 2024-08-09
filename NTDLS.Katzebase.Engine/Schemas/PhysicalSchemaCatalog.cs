using NTDLS.Katzebase.Shared;

namespace NTDLS.Katzebase.Engine.Schemas
{
    public class PhysicalSchemaCatalog
    {
        public List<PhysicalSchema> Collection = new();

        public void Add(PhysicalSchema schema)
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

        public PhysicalSchema? GetByName(string name)
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

        public PhysicalSchema? GetById(Guid id)
        {
            return Collection.FirstOrDefault(o => o.Id == id);
        }

        public PhysicalSchemaCatalog Clone()
        {
            var catalog = new PhysicalSchemaCatalog();

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
