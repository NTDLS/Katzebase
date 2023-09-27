namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbIndex
    {
        public List<KbIndexAttribute> Attributes { get; private set; } = new();
        public string Name { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modfied { get; set; }
        public virtual bool IsUnique { get; set; }
        public uint Partitions { get; set; }

        public KbIndex(string name)
        {
            Name = name;
        }

        public KbIndex(string name, string[] attributes)
        {
            Name = name;
            foreach (var attribute in attributes)
            {
                AddAttribute(attribute);
            }
        }

        public KbIndex(string name, string attributesCsv)
        {
            Name = name;
            foreach (var attribute in attributesCsv.Split(","))
            {
                AddAttribute(attribute);
            }
        }

        public KbIndex() { }

        public void AddAttribute(string name) =>
            AddAttribute(new KbIndexAttribute()
            {
                Field = name
            });


        public void AddAttribute(KbIndexAttribute attribute) => Attributes.Add(attribute);
    }
}
