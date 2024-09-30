namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbQueryRow
    {
        public List<string?> Values { get; set; }

        public KbQueryRow(List<string?> values)
        {
            Values = values;
        }

        public KbQueryRow()
        {
            Values = new();
        }

        public void AddValue(string? value)
        {
            Values.Add(value);
        }
    }
    public class KbQueryRow<TData>
    {
        public List<TData?> Values { get; set; }

        public KbQueryRow(List<TData?> values)
        {
            Values = values;
        }

        public KbQueryRow()
        {
            Values = new();
        }

        public void AddValue(TData? value)
        {
            Values.Add(value);
        }
    }
}
