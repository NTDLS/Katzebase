namespace NTDLS.Katzebase.Engine.Schemas
{
    public class PhysicalSchemaCatalog
    {
        public List<PhysicalSchema> Collection = new();

        public void Add(PhysicalSchema schema)
        {
            this.Collection.Add(schema);
        }

        public bool ContainsName(string name)
        {
            foreach (var item in Collection)
            {
                if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
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
                if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
            return null;
        }

        public PhysicalSchema? GetById(Guid id)
        {
            return (from o in Collection where o.Id == id select o).FirstOrDefault();
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
