namespace Katzebase.Engine.Functions
{
    [Serializable]
    public class PhysicalProcedure
    {
        public List<string> Parameters { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modfied { get; set; }
        public string Body { get; set; } = string.Empty;

        public PhysicalProcedure Clone()
        {
            return new PhysicalProcedure
            {
                Id = Id,
                Name = Name,
                Created = Created,
                Modfied = Modfied
            };
        }

        public void AddParameter(string name)
        {
            Parameters.Add(name);
        }
    }
}
