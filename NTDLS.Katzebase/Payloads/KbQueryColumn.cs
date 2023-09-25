namespace Katzebase.Payloads
{
    public class KbQueryField
    {
        public string Name { get; set; }

        public KbQueryField(string name)
        {
            Name = name;
        }

        public new string ToString() => Name;
    }
}
