namespace Katzebase.PublicLibrary.Payloads
{
    public class KbIndex
    {
        public List<KbIndexAttribute> Attributes { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? Id { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Modfied { get; set; }
        public bool IsUnique { get; set; }

        public KbIndex()
        {
            Attributes = new List<KbIndexAttribute>();
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
