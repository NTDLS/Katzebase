using NTDLS.Helpers;
namespace NTDLS.Katzebase.PersistentTypes.Schema
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


        public PhysicalSchema? GetById(Guid id)
            => Collection.FirstOrDefault(o => o.Id == id);

        public PhysicalSchema? GetByName(string name)
            => Collection.FirstOrDefault(o => o.Name.Is(name));

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
