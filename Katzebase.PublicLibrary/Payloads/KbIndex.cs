namespace Katzebase.PublicLibrary.Payloads
{
    public class KbIndex
    {
        public List<KbIndexAttribute> Attributes { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modfied { get; set; }
        public bool IsUnique { get; set; }

        public KbIndex(string name)
        {
            Name = name;
        }

        public KbIndex()
        {
        }

        public void AddAttribute(string name)
        {
            AddAttribute(new KbIndexAttribute()
            {
                Field = name
            });
        }
        public void AddAttribute(KbIndexAttribute attribute)
        {
            Attributes.Add(attribute);
        }
    }
}
