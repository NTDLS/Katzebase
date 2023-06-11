namespace Katzebase.Engine.Query
{
    public class QueryField
    {
        public string Key { get; private set; }
        public string SchemaAlias { get; private set; }
        public string Alias { get; private set; } //The name used when retuning the field to the client.
        public int Ordinal { get; private set; }

        public QueryField(string key, string schemaAlias, string alias, int ordinal)
        {
            Key = key.ToLower();
            SchemaAlias = schemaAlias.ToLower();
            Alias = alias; //User Friendly, do not ToLower().
            Ordinal = ordinal;
        }
    }
}
