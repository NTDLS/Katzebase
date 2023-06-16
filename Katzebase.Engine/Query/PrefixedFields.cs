namespace Katzebase.Engine.Query
{
    public class PrefixedFields : List<PrefixedField>
    {

        public new PrefixedField Add(PrefixedField field)
        {
            field.Ordinal = this.Count;
            base.Add(field);
            return field;
        }

        public PrefixedField Add(string key)
        {
            var newField = PrefixedField.Parse(key);
            newField.Ordinal = this.Count;
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
