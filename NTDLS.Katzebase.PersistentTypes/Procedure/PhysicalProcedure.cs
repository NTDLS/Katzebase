namespace NTDLS.Katzebase.PersistentTypes.Procedure
{
    [Serializable]
    public class PhysicalProcedure
    {
        public List<PhysicalProcedureParameter> Parameters { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public List<string> Batches { get; set; } = new List<string>();

        public PhysicalProcedure Clone()
        {
            return new PhysicalProcedure
            {
                Id = Id,
                Name = Name,
                Created = Created,
                Modified = Modified
            };
        }
    }
}
