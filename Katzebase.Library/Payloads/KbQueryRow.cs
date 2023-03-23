namespace Katzebase.Library.Payloads
{
    public class KbQueryRow
    {
        public List<string> Values { get; set; }

        public KbQueryRow(List<string> values)
        {
            Values = values;
        }

        public KbQueryRow()
        {
            Values = new List<string>();
        }

        public void Add(string value)
        {
            Values.Add(value);
        }
    }
}
