namespace Katzebase.Payloads
{
    public class KbNameValue<T>
    {
        public string Name { get; set; }
        public T Value { get; set; }

        public KbNameValue(string name, T value)
        {
            Name = name;
            Value = value;
        }
    }
}
