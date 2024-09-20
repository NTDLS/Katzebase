namespace NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes
{
    public class PrefixedField
    {
        public string Prefix { get; set; }
        public string Field { get; set; }
        public string Alias { get; set; } //The name used when retuning the field to the client, do not ToLower().
        public int Ordinal { get; internal set; } = -1;
        public string Key => Prefix == string.Empty ? Field : $"{Prefix}.{Field}";

        public static PrefixedField Parse(string fieldText)
        {
            if (fieldText.Contains('.'))
            {
                var parts = fieldText.Split('.');
                return new PrefixedField(parts[0], parts[1]);
            }
            else
            {
                return new PrefixedField(string.Empty, fieldText);
            }
        }

        public PrefixedField(string prefix, string field)
        {
            Alias = field;
            Prefix = prefix.ToLowerInvariant();
            Field = field.ToLowerInvariant();
        }

        public PrefixedField(string prefix, string field, string alias)
        {
            Alias = alias;
            Prefix = prefix.ToLowerInvariant();
            Field = field.ToLowerInvariant();
        }

        public override bool Equals(object? obj)
        {
            if (obj is PrefixedField other)
            {
                return Key.Equals(other.Key);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }
}
