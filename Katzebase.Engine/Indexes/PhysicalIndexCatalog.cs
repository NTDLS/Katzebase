﻿using Newtonsoft.Json;

namespace Katzebase.Engine.Indexes
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
                if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
            return null;
        }

        public PhysicalIndexCatalog Clone()
        {
            var catalog = new PhysicalIndexCatalog();

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
