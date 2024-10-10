namespace NTDLS.Katzebase.PersistentTypes.Policy
{
    public class PhysicalPolicyCatalog
    {
        public List<PhysicalPolicy> Collection = new();

        public void Remove(PhysicalPolicy item)
            => Collection.Remove(item);

        public void Add(PhysicalPolicy item)
            => Collection.Add(item);
    }
}
