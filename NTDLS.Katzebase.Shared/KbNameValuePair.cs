namespace NTDLS.Katzebase.Shared
{
    public class KbNameValuePair<TKey, TValue>
    {
        public TKey Name { get; set; }
        public TValue Value { get; set; }

        public KbNameValuePair(TKey name, TValue value)
        {
            Name = name;
            Value = value;
        }
    }
}
