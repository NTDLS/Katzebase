namespace Katzebase.Engine.Query
{
    public class PrefixedFields : List<PrefixedField>
    {
        public PrefixedField Add(string key)
        {
            string prefix = string.Empty;
            string field = key;

            if (key.Contains('.'))
            {
                var parts = key.Split('.');
                prefix = parts[0];
                field = parts[1];
            }

            var newField = new PrefixedField(prefix, field)
            {
                Ordinal = this.Count
            };
            this.Add(newField);

            return newField;
        }

        public PrefixedField Add(string prefix, string field)
        {
            var newField = new PrefixedField(prefix, field)
            {
                Ordinal = this.Count
            };

            this.Add(newField);
            return newField;
        }

        public PrefixedField Add(string prefix, string field, string alias)
        {
            var newField = new PrefixedField(prefix, field, alias)
            {
                Ordinal = this.Count
            };

            this.Add(newField);
            return newField;
        }
    }
}
