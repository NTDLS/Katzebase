using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Katzebase.Engine.Indexes
{
    [Serializable]
    public class PersistIndexCatalog
    {
        public List<PersistIndex> Collection = new List<PersistIndex>();

        [JsonIgnore]
        public string? DiskPath { get; set; }

        public void Remove(PersistIndex item)
        {
            Collection.Remove(item);
        }

        public void Add(PersistIndex item)
        {
            Collection.Add(item);
        }

        public PersistIndex? GetById(Guid id)
        {
            return (from o in Collection where o.Id == id select o).FirstOrDefault();
        }

        public PersistIndex? GetByName(string name)
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

        public PersistIndexCatalog Clone()
        {
            var catalog = new PersistIndexCatalog();

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
