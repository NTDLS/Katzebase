namespace Katzebase.Engine.KbLib
{
    public class KbNameValuePair
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public KbNameValuePair(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
