namespace Katzebase.Engine.Query
{
    public class QuerySchema
    {
        public string Key { get; set; }
        public string Alias { get; set; }

        public QuerySchema(string key, string alias)
        {
            Key = key;
            Alias = alias;
        }
    }
}