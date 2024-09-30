namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class PrefixedFields : List<PrefixedField>
    {
        public new PrefixedField Add(PrefixedField field)
        {
            field.Ordinal = Count;
            base.Add(field);
            return field;
        }

        public PrefixedField Add(int? scriptLine, string key)
        {
            var newField = PrefixedField.Parse(scriptLine, key);
            newField.Ordinal = Count;
            Add(newField);
            return newField;
        }

        public PrefixedField Add(int? scriptLine, string prefix, string field)
        {
            var newField = new PrefixedField(scriptLine, prefix, field)
            {
                Ordinal = Count
            };

            Add(newField);
            return newField;
        }

        public PrefixedField Add(int? scriptLine, string prefix, string field, string alias)
        {
            var newField = new PrefixedField(scriptLine, prefix, field, alias)
            {
                Ordinal = Count
            };

            Add(newField);
            return newField;
        }
    }
}
