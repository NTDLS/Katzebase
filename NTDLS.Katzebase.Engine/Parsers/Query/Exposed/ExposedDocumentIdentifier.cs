namespace NTDLS.Katzebase.Engine.Parsers.Query.Exposed
{
    /// <summary>
    /// The "exposed" classes are helpers that allow us to access the ordinal of fields as well as the some of the nester properties.
    /// This one is for fields that are document field names (with optional schema aliases).
    /// </summary>
    internal class ExposedDocumentIdentifier
    {
        public int Ordinal { get; private set; }
        public string Alias { get; private set; }
        public string SchemaAlias { get; private set; }
        public string Name { get; private set; }

        private string? _key = null;
        public string Key
        {
            get
            {
                if (_key == null)
                {
                    if (string.IsNullOrEmpty(SchemaAlias))
                    {
                        _key = Name.ToLowerInvariant();
                    }
                    else
                    {
                        return $"{SchemaAlias}.{Name}".ToLowerInvariant();
                    }
                }

                return _key;
            }
        }

        public ExposedDocumentIdentifier(int ordinal, string alias, string schemaAlias, string name)
        {
            Ordinal = ordinal;
            Alias = alias;
            SchemaAlias = schemaAlias.ToLowerInvariant();
            Name = name;
        }
    }
}
