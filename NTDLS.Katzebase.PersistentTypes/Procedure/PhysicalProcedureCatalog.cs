using Newtonsoft.Json;
using NTDLS.Helpers;

namespace NTDLS.Katzebase.PersistentTypes.Procedure
{
    [Serializable]
    public class PhysicalProcedureCatalog
    {
        public List<PhysicalProcedure> Collection = new();

        [JsonIgnore]
        public string? DiskPath { get; set; }

        public void Remove(PhysicalProcedure item)
            => Collection.Remove(item);

        public void Add(PhysicalProcedure item)
            => Collection.Add(item);

        public PhysicalProcedure? GetById(Guid id)
            => Collection.FirstOrDefault(x => x.Id == id);

        public PhysicalProcedure? GetByName(string name)
            => Collection.FirstOrDefault(o => o.Name.Is(name));

        public PhysicalProcedureCatalog Clone()
        {
            var catalog = new PhysicalProcedureCatalog();

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
