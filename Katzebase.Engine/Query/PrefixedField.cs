namespace Katzebase.Engine.Query
{
    public class PrefixedField
    {
        public string Prefix { get; set; }
        public string Field { get; set; }
        public string Alias { get; private set; } //The name used when retuning the field to the client, do not ToLower().
        public int Ordinal { get; internal set; } = -1;
        public string Key => (Prefix == string.Empty) ? Field : $"{Prefix}.{Field}";

        public PrefixedField(string field)
        {
            Field = field.ToLower();
            Alias = field;
            Prefix = string.Empty;
        }

        public PrefixedField(string prefix, string field)
        {
            Alias = field;
            Prefix = prefix.ToLower();
            Field = field.ToLower();
        }

        public PrefixedField(string prefix, string field, string alias)
        {
            Alias = alias;
            Prefix = prefix.ToLower();
            Field = field.ToLower();
        }
    }
}
