namespace NTDLS.Katzebase.Parsers.Query.SupportingTypes
{
    public class PrefixedField
    {
        /// <summary>
        /// The alias of the schema that the field is in reference to.
        /// </summary>
        public string SchemaPrefix { get; set; }

        /// <summary>
        /// The name of the field which is being referenced.
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// The name used when retuning the field to the client, do not ToLower().
        /// </summary>
        public string FieldAlias { get; set; }

        /// <summary>
        /// The index of the field in a collection.
        /// </summary>
        public int Ordinal { get; internal set; } = -1;

        /// <summary>
        /// The key is the SchemaPrefix.FieldName when the schema prefix is present, otherwise it is the FieldName.
        /// </summary>
        public string Key => SchemaPrefix == string.Empty ? FieldName : $"{SchemaPrefix}.{FieldName}";

        /// <summary>
        /// If applicable, this is the line from the script that this expression is derived from.
        /// </summary>
        public int? ScriptLine { get; set; }

        /// <summary>
        /// Parses a dot schema prefixed field name.
        /// </summary>
        public static PrefixedField Parse(int? scriptLine, string fieldText)
        {
            if (fieldText.Contains('.'))
            {
                var parts = fieldText.Split('.');
                return new PrefixedField(scriptLine, parts[0], parts[1]);
            }
            else
            {
                return new PrefixedField(scriptLine, string.Empty, fieldText);
            }
        }

        public PrefixedField(int? scriptLine, string prefix, string field)
        {
            ScriptLine = scriptLine;
            FieldAlias = field;
            SchemaPrefix = prefix.ToLowerInvariant();
            FieldName = field.ToLowerInvariant();
        }

        public PrefixedField(int? scriptLine, string prefix, string field, string alias)
        {
            FieldAlias = alias;
            SchemaPrefix = prefix.ToLowerInvariant();
            FieldName = field.ToLowerInvariant();
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
