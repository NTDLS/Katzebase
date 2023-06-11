namespace Katzebase.Engine.Query.Constraints
{
    public class PrefixedField
    {
        public string Prefix { get; set; }
        public string Field { get; set; }
        public string Key => $"{Prefix}.{Field}";

        public PrefixedField(string prefix, string field)
        {
            Prefix = prefix.ToLower();
            Field = field.ToLower();
        }
    }
}
