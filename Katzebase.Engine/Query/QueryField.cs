namespace Katzebase.Engine.Query
{
    public class QueryField
    {
        public string Key { get; set; }
        public string SchemaAlias { get; set; }
        public string Alias { get; set; } //The name used when retuning the field to the client.

        public QueryField(string key, string schemaAlias, string alias)
        {
            Key = key.ToLower();
            SchemaAlias = schemaAlias.ToLower();
            Alias = alias; //User Friendly, do not ToLower().
        }
    }
}
