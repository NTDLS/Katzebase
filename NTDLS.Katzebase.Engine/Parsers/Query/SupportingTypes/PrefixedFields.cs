namespace NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes
{
    public class PrefixedFields : List<PrefixedField>
    {
        public new PrefixedField Add(PrefixedField field)
        {
            field.Ordinal = Count;
            base.Add(field);
            return field;
        }

        public PrefixedField Add(string key)
        {
            var newField = PrefixedField.Parse(key);
            newField.Ordinal = Count;
            Add(newField);
            return newField;
        }

        public PrefixedField Add(string prefix, string field)
        {
            var newField = new PrefixedField(prefix, field)
            {
                Ordinal = Count
            };

            Add(newField);
            return newField;
        }

        public PrefixedField Add(string prefix, string field, string alias)
        {
            var newField = new PrefixedField(prefix, field, alias)
            {
                Ordinal = Count
            };

            Add(newField);
            return newField;
        }
    }
}
