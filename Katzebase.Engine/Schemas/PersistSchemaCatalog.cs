namespace Katzebase.Engine.Schemas
{
    public class PersistSchemaCatalog
    {
        public List<PersistSchema> Collection = new List<PersistSchema>();

        public void Add(PersistSchema namespaceMeta)
        {
            this.Collection.Add(namespaceMeta);
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

        public PersistSchema? GetByName(string name)
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

        public PersistSchema? GetById(Guid id)
        {
            return (from o in Collection where o.Id == id select o).FirstOrDefault();
        }

        public PersistSchemaCatalog Clone()
        {
            var catalog = new PersistSchemaCatalog();

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
